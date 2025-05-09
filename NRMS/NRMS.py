import warnings
warnings.filterwarnings('ignore')
import numpy as np
import pandas as pd
import tensorflow as tf
from keras.layers import *
from keras.models import Model
from nltk.tokenize import word_tokenize
from typing import Dict, List, Tuple, Any


def load_news_data(file_path: str) -> Dict[str, List[str]]:
    """加载新闻数据并分词
    
    Args:
        file_path: 新闻数据文件路径
        
    Returns:
        Dict[str, List[str]]: 新闻ID到分词后标题的映射
    """
    news_data = {}
    with open(file_path, 'r', encoding='utf-8') as file:
        for line in file:
            parts = line.strip().split('\t')
            news_id = parts[0]
            content = word_tokenize(parts[3].lower())
            news_data[news_id] = content
    return news_data


def preprocess_news(news_dict: Dict[str, List[str]]) -> Tuple[np.ndarray, Dict[str, int]]:
    """预处理新闻数据，构建词表并转换为ID序列
    
    Args:
        news_dict: 新闻ID到分词后标题的映射
        
    Returns:
        Tuple[np.ndarray, Dict[str, int]]: 新闻标题矩阵和词表
    """
    word_to_id = {'PADDING': 0}
    news_title_matrix = [[0] * 30]  # news_id = 0为占位符
    
    for news_id in news_dict:
        title_ids = []
        for word in news_dict[news_id]:
            if word not in word_to_id:
                word_to_id[word] = len(word_to_id)
            title_ids.append(word_to_id[word])
        title_ids = title_ids[:30] + [0] * (30 - len(title_ids))
        news_title_matrix.append(title_ids)
        
    return np.array(news_title_matrix, dtype='int32'), word_to_id


def build_models(max_sent_length: int, max_sents: int, word_dict_size: int) -> Tuple[Model, Model]:
    """构建新闻编码器和用户编码器模型
    
    Args:
        max_sent_length: 输入新闻最大长度
        max_sents: 用户历史最大长度
        word_dict_size: 词典大小
        
    Returns:
        Tuple[Model, Model]: 训练模型和测试模型
    """
    # 新闻编码器
    title_input = Input(shape=(max_sent_length,), dtype='int32', name='candidate_input')
    embedding_layer = Embedding(word_dict_size, 300, mask_zero=True, trainable=True)
    embedded_sequences = embedding_layer(title_input)
    dropout_emb = Dropout(0.2)(embedded_sequences)
    self_attention = MultiHeadAttention(num_heads=20, key_dim=15)(dropout_emb, dropout_emb, dropout_emb)
    self_attention = Dropout(0.2)(self_attention)
    attention = Dense(200, activation='tanh')(self_attention)
    attention = Flatten()(Dense(1)(attention))
    attention_weight = Activation('softmax')(attention)
    title_representation = Dot(axes=(1,1))([self_attention, attention_weight])
    title_encoder = Model(title_input, title_representation)
    
    # 用户编码器
    news_input = Input((max_sents, max_sent_length), name='history_input')
    news_encoders = TimeDistributed(title_encoder)(news_input)
    news_attention = MultiHeadAttention(num_heads=20, key_dim=15)(news_encoders, news_encoders, news_encoders)
    news_encoders = Dropout(0.2)(news_attention)
    
    # 训练模型
    candidates = Input((1 + 4, max_sent_length), name='train_candidates')
    candidate_vectors = TimeDistributed(title_encoder)(candidates)
    
    # 计算用户表示
    news_attention = Dense(200, activation='tanh')(news_encoders)
    news_attention = Flatten()(Dense(1)(news_attention))
    news_attention_weight = Activation('softmax')(news_attention)
    user_representation = Dot(axes=(1, 1))([news_encoders, news_attention_weight])
    
    # 计算分数
    logits = dot([user_representation, candidate_vectors], axes=-1)
    logits = Activation('softmax')(logits)
    train_model = Model([candidates, news_input], logits)
    train_model.compile(loss='categorical_crossentropy', optimizer='adam', metrics=['acc'])
    
    # 测试模型
    candidate_one = Input((max_sent_length,), name='test_candidate')
    candidate_vector = title_encoder(candidate_one)
    score = Activation('sigmoid')(dot([user_representation, candidate_vector], axes=-1))
    test_model = Model([candidate_one, news_input], score)
    
    return train_model, test_model


def load_train_data(train_file: str, news_id_map: Dict[str, int]) -> Tuple[np.ndarray, np.ndarray, np.ndarray]:
    """加载训练数据
    
    Args:
        train_file: 训练数据文件路径
        news_id_map: 新闻ID到索引的映射
        
    Returns:
        Tuple[np.ndarray, np.ndarray, np.ndarray]: 候选新闻、标签和用户历史
    """
    train_candidates = []
    train_labels = []
    train_user_history = []
    negative_ratio = 4
    
    with open(train_file, 'r') as file:
        for line in file:
            parts = line.strip().split('\t')
            if len(parts) < 5:
                continue
                
            click_history = parts[3].split()
            pos_neg_docs = parts[4]
            
            # 处理用户历史
            click_ids = [news_id_map[x] for x in click_history if x in news_id_map][-50:]
            click_ids += [0] * (50 - len(click_ids))
            
            # 处理正负例
            pos_neg_pairs = pos_neg_docs.split()
            positive_docs = []
            negative_docs = []
            for item in pos_neg_pairs:
                doc, label = item.split('-')
                if label == '1':
                    positive_docs.append(doc)
                else:
                    negative_docs.append(doc)
                    
            for pos_doc in positive_docs:
                if pos_doc not in news_id_map or len(negative_docs) < negative_ratio:
                    continue
                    
                neg_samples = np.random.choice(negative_docs, negative_ratio, replace=True).tolist()
                candidates = neg_samples + [pos_doc]
                shuffled = np.random.permutation(negative_ratio + 1)
                candidates = [candidates[i] for i in shuffled]
                labels = [0] * negative_ratio + [1]
                labels = [labels[i] for i in shuffled]
                
                candidate_ids = [news_id_map[doc] for doc in candidates]
                train_candidates.append(candidate_ids)
                train_labels.append(labels)
                train_user_history.append(click_ids)
                
    return (np.array(train_candidates, dtype='int32'), 
            np.array(train_labels, dtype='int32'), 
            np.array(train_user_history, dtype='int32'))


def load_test_data(valid_file: str, news_id_map: Dict[str, int], news_title_matrix: np.ndarray) -> Tuple[np.ndarray, np.ndarray, List[Tuple[int, int]], List[List[Any]]]:
    """加载测试数据
    
    Args:
        valid_file: 验证数据文件路径
        news_id_map: 新闻ID到索引的映射
        news_title_matrix: 新闻标题矩阵
        
    Returns:
        Tuple[np.ndarray, np.ndarray, List[Tuple[int, int]], List[List[Any]]]: 
            测试候选新闻、用户历史、测试索引和测试会话
    """
    test_candidates = []
    test_user_history = []
    test_indices = []
    test_sessions = []
    
    with open(valid_file, 'r', encoding='utf-8') as file:
        for line in file:
            parts = line.strip().split('\t')
            if len(parts) < 5:
                continue
                
            user_id = parts[0]
            click_history = parts[3].split()
            docs_part = parts[4].split(' ')
            
            # 处理用户历史
            click_ids = [news_id_map[x] for x in click_history if x in news_id_map][-50:]
            click_ids += [0] * (50 - len(click_ids))
            
            # 处理候选新闻
            valid_docs = []
            for doc in docs_part:
                doc_id = doc.split('-')[0]
                if doc_id in news_id_map:
                    valid_docs.append(doc_id)
            
            if not valid_docs:
                continue
                
            test_sessions.append([user_id, valid_docs, parts[2]])
            start_idx = len(test_candidates)
            
            for doc_id in valid_docs:
                test_candidates.append(news_id_map[doc_id])
                test_user_history.append(click_ids)
                
            end_idx = len(test_candidates)
            if start_idx < end_idx:
                test_indices.append((start_idx, end_idx))
    
    return (np.array(test_candidates, dtype='int32'),
            np.array(test_user_history, dtype='int32'),
            test_indices,
            test_sessions)


def generate_batch_data_random(batch_size: int, news_title: np.ndarray, 
                             train_candidates: np.ndarray, train_labels: np.ndarray, 
                             train_user_history: np.ndarray) -> tf.data.Dataset:
    """生成随机批次的训练数据
    
    Args:
        batch_size: 批次大小
        news_title: 新闻标题矩阵
        train_candidates: 训练候选新闻
        train_labels: 训练标签
        train_user_history: 用户历史
        
    Returns:
        tf.data.Dataset: 训练数据集
    """
    indices = np.arange(len(train_labels))
    np.random.shuffle(indices)
    labels = train_labels
    batches = [indices[range(batch_size*i, min(len(labels), batch_size*(i+1)))] 
              for i in range(len(labels)//batch_size+1)]
    
    def generator():
        for batch_indices in batches:
            if len(batch_indices) == 0:
                continue
                
            candidate_indices = train_candidates[batch_indices].reshape(-1).tolist()
            user_indices = train_user_history[batch_indices].reshape(-1).tolist()
            
            items = tf.gather(news_title, candidate_indices)
            items = tf.reshape(items, [-1, 5, 30])
            
            users = tf.gather(news_title, user_indices)
            users = tf.reshape(users, [-1, 50, 30])
            
            batch_labels = tf.convert_to_tensor(labels[batch_indices], dtype=tf.float32)
            yield (items, users), batch_labels
    
    output_signature = (
        (tf.TensorSpec(shape=(None, 5, 30), dtype=tf.int32),
         tf.TensorSpec(shape=(None, 50, 30), dtype=tf.int32)),
        tf.TensorSpec(shape=(None, 5), dtype=tf.float32)
    )
    
    return tf.data.Dataset.from_generator(
        generator,
        output_signature=output_signature
    )


def generate_batch_data(batch_size: int, news_title: np.ndarray, 
                       test_candidates: np.ndarray, test_user_history: np.ndarray) -> tf.data.Dataset:
    """生成批次的测试数据
    
    Args:
        batch_size: 批次大小
        news_title: 新闻标题矩阵
        test_candidates: 测试候选新闻
        test_user_history: 用户历史
        
    Returns:
        tf.data.Dataset: 测试数据集
    """
    valid_indices = np.where(test_candidates < len(news_title))[0]
    test_candidates = test_candidates[valid_indices]
    test_user_history = test_user_history[valid_indices]
    
    indices = np.arange(len(test_candidates))
    batches = [indices[range(batch_size*i, min(len(indices), batch_size*(i+1)))] 
              for i in range(len(indices)//batch_size+1)]
    
    def generator():
        for batch_indices in batches:
            if len(batch_indices) == 0:
                continue
                
            batch_candidates = test_candidates[batch_indices].tolist()
            batch_user_history = test_user_history[batch_indices].tolist()
            
            items = tf.gather(news_title, batch_candidates)
            users = tf.gather(news_title, batch_user_history)
            
            items = tf.reshape(items, [-1, 30])
            users = tf.reshape(users, [-1, 50, 30])
            
            yield {"test_candidate": items, "history_input": users}
    
    output_signature = {
        "test_candidate": tf.TensorSpec(shape=(None, 30), dtype=tf.int32),
        "history_input": tf.TensorSpec(shape=(None, 50, 30), dtype=tf.int32)
    }
    
    return tf.data.Dataset.from_generator(
        generator,
        output_signature=output_signature
    )


def main():
    """主函数"""
    print("="*50)
    print("开始加载数据...")
    print("="*50)
    
    # 加载新闻数据
    news_data = load_news_data('/kaggle/input/nrmstiny/dev_news.tsv')
    print(f"成功加载 {len(news_data)} 条新闻数据")
    
    # 预处理新闻数据
    news_title_matrix, word_to_id = preprocess_news(news_data)
    print(f"新闻标题矩阵形状: {news_title_matrix.shape}")
    print(f"词表大小: {len(word_to_id)}")
    
    # 构建新闻ID映射
    news_id_map = {news_id: idx for idx, news_id in enumerate(news_data)}
    print(f"新闻ID映射大小: {len(news_id_map)}")
    
    # 加载训练数据
    train_candidates, train_labels, train_user_history = load_train_data(
        '/kaggle/input/nrmstiny/dev_behaviors.tsv',
        news_id_map
    )
    print("\n训练数据统计:")
    print(f"- 候选新闻数: {len(train_candidates)}")
    print(f"- 标签数: {len(train_labels)}")
    print(f"- 用户历史数: {len(train_user_history)}")
    
    # 加载测试数据
    test_candidates, test_user_history, test_indices, test_sessions = load_test_data(
        '/kaggle/input/nrmstiny/dev_behaviors.tsv',
        news_id_map,
        news_title_matrix
    )
    print("\n测试数据统计:")
    print(f"- 候选新闻数: {len(test_candidates)}")
    print(f"- 用户历史数: {len(test_user_history)}")
    print(f"- 测试索引数: {len(test_indices)}")
    print(f"- 会话数: {len(test_sessions)}")
    
    if len(test_candidates) == 0:
        print("警告: 测试数据为空")
        return
        
    # 构建模型
    print("\n" + "="*50)
    print("开始构建模型...")
    print("="*50)
    
    news_title_matrix = tf.constant(news_title_matrix, dtype=tf.int32)
    train_model, test_model = build_models(30, 50, len(word_to_id))
    
    # 训练模型
    print("\n" + "="*50)
    print("开始训练模型...")
    print("="*50)
    
    for epoch in range(10):
        print(f"\nEpoch {epoch + 1}/1")
        train_dataset = generate_batch_data_random(30, news_title_matrix, train_candidates, train_labels, train_user_history)
        steps = max(1, len(train_labels) // 30)
        train_model.fit(train_dataset, epochs=1, steps_per_epoch=steps)
    
    print("\n模型训练完成")
    
    # 编码新闻
    print("\n" + "="*50)
    print("开始编码新闻...")
    print("="*50)
    
    all_news_encoded = []
    batch_size = 100
    total_news = len(news_title_matrix)
    title_encoder = test_model.layers[10]
    
    for i in range(0, total_news, batch_size):
        batch_news = news_title_matrix[i:i+batch_size]
        batch_encoded = title_encoder(batch_news)
        all_news_encoded.append(batch_encoded)
        if (i // batch_size) % 10 == 0:
            print(f"已处理 {i}/{total_news} 条新闻")
            
    all_news_encoded = tf.concat(all_news_encoded, axis=0)
    print(f"\n新闻编码完成，形状: {all_news_encoded.shape}")
    
    # 生成推荐
    print("\n" + "="*50)
    print("开始生成推荐...")
    print("="*50)
    
    results = []
    news_ids = list(news_data.keys())
    processed_users = set()
    
    for i in range(len(test_indices)):
        session = test_sessions[i]
        user_id = session[0]
        
        if user_id in processed_users:
            continue
        processed_users.add(user_id)
        
        user_history = test_user_history[test_indices[i][0]]
        if len(user_history) == 0:
            print(f"警告: 用户 {user_id} 没有历史记录")
            continue
            
        user_history_news = tf.gather(news_title_matrix, user_history)
        user_history_news = tf.reshape(user_history_news, [1, 50, 30])
        
        user_representation = test_model.layers[1](user_history_news)
        user_representation = test_model.layers[2](user_representation, user_representation, user_representation)
        user_representation = test_model.layers[3](user_representation)
        
        scores = tf.matmul(user_representation, all_news_encoded, transpose_b=True)
        scores = tf.squeeze(scores).numpy()
        
        if scores.ndim > 1:
            scores = scores.mean(axis=0)
        
        top10_indices = np.argsort(scores)[-10:][::-1]
        valid_indices = [idx for idx in top10_indices if idx < len(news_ids)]
        
        if not valid_indices:
            print(f"警告: 用户 {user_id} 没有有效的推荐索引")
            continue
            
        top10_news_ids = [news_ids[idx] for idx in valid_indices]
        
        results.append({
            'user_id': user_id,
            'recommended_news': ' '.join(top10_news_ids)
        })
        
        if len(results) % 100 == 0:
            print(f"已处理 {len(results)} 个用户")
    
    print(f"\n推荐生成完成，共生成 {len(results)} 条推荐")
    
    # 保存结果
    print("\n" + "="*50)
    print("保存推荐结果...")
    print("="*50)
    
    df = pd.DataFrame(results)
    df.to_csv('/kaggle/working/recommendations.csv', index=False)
    print("推荐结果已保存到 recommendations.csv")


if __name__ == "__main__":
    main()