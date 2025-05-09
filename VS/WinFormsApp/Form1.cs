using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Data.SqlClient;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Web;
using MailKit.Security;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MailKit.Net.Smtp;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        private Button personalizedRecommendationButton = new Button();
        private Button searchNewsButton = new Button();
        private Button categorySearchButton = new Button();
        private Button contentManagementButton = new Button();
        private Button logoutButton = new Button();
        private Button systemUpdateButton = new Button();

        private TextBox emailInputTextBox = new TextBox();
        private Button sendLoginLinkButton = new Button();
        private Label loginStatusLabel = new Label();

        private Panel loginPanel = new Panel();
        private Panel mainPanel = new Panel();
        private WebBrowser newsBrowser = new WebBrowser();
        private Label systemTitleLabel = new Label();

        // 邮件发送配置
        private static readonly string SmtpHost = "smtp.qq.com";
        private static readonly int SmtpPort = 587;
        private static readonly string SmtpUsername = "3122628687@qq.com";
        private static readonly string SmtpPassword = "rxfskkhvdmxyddci";

        private static string JwtSecret = "eyJhbGciOiJIUzUxMiIsInR5cCI6IkpXVCJ9.eyJuYW1lIjoiSm9obiIsImFkbWluIjp0cnVlfQ.OI5ht9P98GhFNixYpJHkwDM30NQDTcBE_XqDtmZCHn_QR1IA91S6X0A7UFCjog6Le6VlpKVXoeuS8GGbC-wYsQ";
        private string jwtValidationToken = "";
        private int currentUserId = -1;

        private HttpListener httpListener;
        private bool isListening = false;
        private int port = 8080;

        public Form1()
        {
            InitializeComponent();
            StartHttpServer();

            // 设置主窗口大小和样式
            this.Size = new Size(1000, 700);
            this.MinimumSize = new Size(800, 600);
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.Font = new Font("Microsoft YaHei", 10);

            // 创建系统标题
            systemTitleLabel = new Label
            {
                Text = "校园新闻推荐系统",
                Font = new Font("Microsoft YaHei", 28, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Dock = DockStyle.Top,
                Height = 80,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                BackColor = Color.White
            };
            this.Controls.Add(systemTitleLabel);

            // 设置登录面板
            loginPanel.Dock = DockStyle.Fill;
            loginPanel.BackColor = Color.White;
            loginPanel.Margin = new Padding(0, 80, 0, 0);

            // 创建登录标题
            Label loginTitle = new Label
            {
                Text = "用户登录",
                Font = new Font("Microsoft YaHei", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 120, 215),
                Location = new Point((this.ClientSize.Width - 200) / 2, 100),
                Size = new Size(200, 40),
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter
            };
            loginPanel.Controls.Add(loginTitle);

            // 设置邮箱输入框
            emailInputTextBox.Location = new Point((this.ClientSize.Width - 400) / 2, 180);
            emailInputTextBox.Size = new Size(400, 40);
            emailInputTextBox.Font = new Font("Microsoft YaHei", 12);
            emailInputTextBox.PlaceholderText = "请输入邮箱";
            emailInputTextBox.BorderStyle = BorderStyle.FixedSingle;
            emailInputTextBox.Padding = new Padding(5);
            loginPanel.Controls.Add(emailInputTextBox);

            // 设置发送按钮
            sendLoginLinkButton.Location = new Point((this.ClientSize.Width - 400) / 2, 240);
            sendLoginLinkButton.Size = new Size(400, 45);
            sendLoginLinkButton.Text = "发送登录链接";
            sendLoginLinkButton.Font = new Font("Microsoft YaHei", 12, FontStyle.Bold);
            sendLoginLinkButton.BackColor = Color.FromArgb(0, 120, 215);
            sendLoginLinkButton.ForeColor = Color.White;
            sendLoginLinkButton.FlatStyle = FlatStyle.Flat;
            sendLoginLinkButton.FlatAppearance.BorderSize = 0;
            sendLoginLinkButton.Cursor = Cursors.Hand;
            sendLoginLinkButton.Click += new EventHandler(sendLoginLinkButton_Click);
            loginPanel.Controls.Add(sendLoginLinkButton);

            // 设置状态标签
            loginStatusLabel.Location = new Point((this.ClientSize.Width - 400) / 2, 300);
            loginStatusLabel.Size = new Size(400, 50);  // 增加高度以容纳多行文本
            loginStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            loginStatusLabel.Font = new Font("Microsoft YaHei", 12);  // 增大字体
            loginStatusLabel.ForeColor = Color.Gray;
            loginStatusLabel.Text = "等待登录...";
            loginStatusLabel.AutoSize = false;  // 禁用自动大小
            loginStatusLabel.Padding = new Padding(5);  // 添加内边距
            loginStatusLabel.BackColor = Color.FromArgb(245, 245, 245);  // 添加浅灰色背景
            loginStatusLabel.BorderStyle = BorderStyle.FixedSingle;  // 添加边框
            loginPanel.Controls.Add(loginStatusLabel);

            // 添加Resize事件处理
            this.Resize += (s, e) => {
                loginTitle.Location = new Point((this.ClientSize.Width - 200) / 2, 100);
                emailInputTextBox.Location = new Point((this.ClientSize.Width - 400) / 2, 180);
                sendLoginLinkButton.Location = new Point((this.ClientSize.Width - 400) / 2, 240);
                loginStatusLabel.Location = new Point((this.ClientSize.Width - 400) / 2, 300);
            };

            // 设置主面板
            mainPanel.Dock = DockStyle.Fill;
            mainPanel.BackColor = Color.White;
            mainPanel.Padding = new Padding(20);
            mainPanel.Margin = new Padding(0, 100, 0, 0);  // 顶部留出标题空间

            // 创建右侧按钮面板
            Panel rightButtonPanel = new Panel
            {
                Dock = DockStyle.Right,
                Width = 200,
                BackColor = Color.FromArgb(245, 245, 245),
                Padding = new Padding(10)
            };

            // 设置按钮样式
            personalizedRecommendationButton.Text = "个性推荐";
            searchNewsButton.Text = "搜索新闻";
            categorySearchButton.Text = "分类检索";
            contentManagementButton.Text = "内容管理";
            logoutButton.Text = "退出登录";
            systemUpdateButton.Text = "系统更新";

            int buttonHeight = 40;
            int buttonSpacing = 30;
            int startY = 20;

            // 创建按钮容器
            Panel buttonContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(10, 20, 10, 20),
                AutoScroll = true
            };

            // 设置按钮位置
            personalizedRecommendationButton.Location = new Point(10, startY);
            searchNewsButton.Location = new Point(10, startY + buttonHeight + buttonSpacing);
            categorySearchButton.Location = new Point(10, startY + (buttonHeight + buttonSpacing) * 2);
            contentManagementButton.Location = new Point(10, startY + (buttonHeight + buttonSpacing) * 3);
            logoutButton.Location = new Point(10, startY + (buttonHeight + buttonSpacing) * 4);
            systemUpdateButton.Location = new Point(10, startY + (buttonHeight + buttonSpacing) * 5);

            foreach (Button btn in new[] { personalizedRecommendationButton, searchNewsButton, categorySearchButton, contentManagementButton, logoutButton, systemUpdateButton })
            {
                btn.Font = new Font("Microsoft YaHei", 12, FontStyle.Bold);
                btn.FlatStyle = FlatStyle.Flat;
                btn.BackColor = Color.FromArgb(0, 120, 215);
                btn.ForeColor = Color.White;
                btn.FlatAppearance.BorderSize = 0;
                btn.Height = buttonHeight;
                btn.Width = 180;
                btn.Cursor = Cursors.Hand;
                buttonContainer.Controls.Add(btn);
            }

            rightButtonPanel.Controls.Add(buttonContainer);

            // 设置结果浏览器
            newsBrowser.Dock = DockStyle.Fill;
            newsBrowser.ScriptErrorsSuppressed = true;
            newsBrowser.DocumentCompleted += result_DocumentCompleted;
            mainPanel.Controls.Add(newsBrowser);
            mainPanel.Controls.Add(rightButtonPanel);

            // 添加按钮点击事件
            personalizedRecommendationButton.Click += new EventHandler(personalizedRecommendationButton_Click);
            searchNewsButton.Click += new EventHandler(searchNewsButton_Click);
            categorySearchButton.Click += new EventHandler(categorySearchButton_Click);
            contentManagementButton.Click += new EventHandler(contentManagementButton_Click);
            logoutButton.Click += new EventHandler(logoutButton_Click);
            systemUpdateButton.Click += new EventHandler(systemUpdateButton_Click);

            this.Controls.Add(loginPanel);
            this.Controls.Add(mainPanel);

            mainPanel.Visible = false;
        }

        private string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("'", "&#39;")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n");
        }


        private string GetFuzzySearchResults(string userInput)
        {
            string connStr = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=admin;Integrated Security=True";
            var htmlResults = new StringBuilder();
            int resultCount = 0;

            htmlResults.Append("<html><head><style>");
            htmlResults.Append("body { font-family: Arial, sans-serif; margin: 0; padding: 10px; }");
            htmlResults.Append(".result-item { margin-bottom: 20px; padding: 10px; border-left: 4px solid #007bff; cursor: pointer; }");
            htmlResults.Append(".result-item:hover { background-color: #f8f9fa; }");
            htmlResults.Append(".title { font-size: 18px; color: #007bff; margin-bottom: 8px; }");
            htmlResults.Append(".summary { font-size: 14px; color: #666; line-height: 1.5; }");
            htmlResults.Append(".no-results { text-align: center; color: #666; padding: 20px; }");
            htmlResults.Append("</style></head><body>");

            using (SqlConnection connection = new SqlConnection(connStr))
            {
                connection.Open();
                string query = "SELECT TOP 10 newsid, title, abstract FROM News WHERE title LIKE @searchText OR abstract LIKE @searchText";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@searchText", "%" + userInput + "%");

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read() && resultCount < 10)
                        {
                            string newsId = reader["newsid"].ToString();
                            string title = reader["title"].ToString();
                            string summary = reader["abstract"].ToString();

                            if (!string.IsNullOrEmpty(newsId) && !string.IsNullOrEmpty(title))
                            {
                                // 转义HTML特殊字符
                                title = System.Web.HttpUtility.HtmlEncode(title);
                                summary = System.Web.HttpUtility.HtmlEncode(summary);
                                newsId = System.Web.HttpUtility.HtmlEncode(newsId);

                                htmlResults.Append($"<div class='result-item' onclick='window.external.ShowNewsDetail(\"{newsId}\", \"{title}\", \"{summary}\")'>");
                                htmlResults.Append($"<div class='title'>{title}</div>");
                                htmlResults.Append($"<div class='summary'>{summary}</div>");
                                htmlResults.Append("</div>");
                            }
                            resultCount++;
                        }
                    }
                }
            }

            if (resultCount == 0)
            {
                htmlResults.Append("<div class='no-results'>未找到相关结果</div>");
            }

            htmlResults.Append("</body></html>");
            return htmlResults.ToString();
        }

        private void result_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (newsBrowser.Document != null)
            {
                newsBrowser.ObjectForScripting = new NewsExternalInterface(this);
            }
        }

        public class NewsExternalInterface
        {
            private Form1 form;

            public NewsExternalInterface(Form1 form)
            {
                this.form = form;
            }

            public void ShowNewsDetail(string newsId, string title, string content)
            {
                try
                {
                    // 解码HTML实体
                    title = System.Web.HttpUtility.HtmlDecode(title);
                    content = System.Web.HttpUtility.HtmlDecode(content);
                    
                    var detailForm = new NewsDetailForm(title, content);
                    detailForm.Show();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"显示新闻详情时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }


        private void personalizedRecommendationButton_Click(object sender, EventArgs e)
        {
            try
            {
                if (currentUserId == -1)
                {
                    MessageBox.Show("请先登录系统", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string connStr = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=admin;Integrated Security=True";
                
                using (SqlConnection connection = new SqlConnection(connStr))
                {
                    connection.Open();
                    
                    // 获取用户浏览记录
                    string query = "SELECT Impression FROM UserImpressions WHERE UserId = @UserId";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@UserId", currentUserId);
                        var impressions = command.ExecuteScalar() as string;
                        
                        var htmlResults = new StringBuilder();
                        
                        // 添加HTML头部和基本样式
                        htmlResults.Append("<html><head><style>");
                        htmlResults.Append("body { font-family: Arial, sans-serif; margin: 0; padding: 10px; }");
                        htmlResults.Append(".result-item { margin-bottom: 20px; padding: 10px; border-left: 4px solid #007bff; }");
                        htmlResults.Append(".title { font-size: 18px; color: #007bff; margin-bottom: 8px; }");
                        htmlResults.Append(".summary { font-size: 14px; color: #666; line-height: 1.5; }");
                        htmlResults.Append(".no-results { text-align: center; color: #666; padding: 20px; }");
                        htmlResults.Append("</style></head><body>");
                        
                        if (impressions != null)
                        {
                            var newsIds = impressions.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            
                            // 获取新闻信息
                            foreach (var newsId in newsIds.Distinct())
                            {
                                string newsQuery = "SELECT Title, Abstract FROM News WHERE NewsId = @NewsId";
                                using (SqlCommand newsCommand = new SqlCommand(newsQuery, connection))
                                {
                                    newsCommand.Parameters.AddWithValue("@NewsId", newsId);
                                    using (SqlDataReader reader = newsCommand.ExecuteReader())
                                    {
                                        if (reader.Read())
                                        {
                                            string title = reader["Title"].ToString();
                                            string summary = reader["Abstract"].ToString();
                                            
                                            if (!string.IsNullOrEmpty(newsId) && !string.IsNullOrEmpty(title))
                                            {
                                                // 转义特殊字符
                                                string escapedTitle = System.Web.HttpUtility.JavaScriptStringEncode(title);
                                                string escapedSummary = System.Web.HttpUtility.JavaScriptStringEncode(summary);
                                                string escapedNewsId = System.Web.HttpUtility.JavaScriptStringEncode(newsId);
                                                
                                                htmlResults.Append($"<div class='result-item' onclick='window.external.ShowNewsDetail(\"{escapedNewsId}\", \"{escapedTitle}\", \"{escapedSummary}\")'>");
                                                htmlResults.Append($"<div class='title'>{title}</div>");
                                                htmlResults.Append($"<div class='summary'>{summary}</div>");
                                                htmlResults.Append("</div>");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // 如果未找到浏览记录，随机获取10条新闻
                            string randomNewsQuery = "SELECT TOP 10 NewsId, Title, Abstract FROM News ORDER BY NEWID()";
                            using (SqlCommand randomCommand = new SqlCommand(randomNewsQuery, connection))
                            {
                                using (SqlDataReader reader = randomCommand.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        string newsId = reader["NewsId"].ToString();
                                        string title = reader["Title"].ToString();
                                        string summary = reader["Abstract"].ToString();
                                        
                                        if (!string.IsNullOrEmpty(newsId) && !string.IsNullOrEmpty(title))
                                        {
                                            // 转义特殊字符
                                            string escapedTitle = System.Web.HttpUtility.JavaScriptStringEncode(title);
                                            string escapedSummary = System.Web.HttpUtility.JavaScriptStringEncode(summary);
                                            string escapedNewsId = System.Web.HttpUtility.JavaScriptStringEncode(newsId);
                                            
                                            htmlResults.Append($"<div class='result-item' onclick='window.external.ShowNewsDetail(\"{escapedNewsId}\", \"{escapedTitle}\", \"{escapedSummary}\")'>");
                                            htmlResults.Append($"<div class='title'>{title}</div>");
                                            htmlResults.Append($"<div class='summary'>{summary}</div>");
                                            htmlResults.Append("</div>");
                                        }
                                    }
                                }
                            }
                        }
                        
                        htmlResults.Append("</body></html>");
                        newsBrowser.DocumentText = htmlResults.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"获取推荐内容时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void searchNewsButton_Click(object sender, EventArgs e)
        {
            Form searchForm = new Form
            {
                Text = "搜索新闻",
                Size = new Size(400, 200),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            TextBox searchTextBox = new TextBox
            {
                Location = new Point(20, 20),
                Size = new Size(340, 30),
                Font = new Font("Arial", 12),
                PlaceholderText = "请输入搜索关键词"
            };

            Button searchButton = new Button
            {
                Text = "搜索",
                Location = new Point(150, 70),
                Size = new Size(100, 40),
                Font = new Font("Arial", 12)
            };

            searchButton.Click += (s, ev) =>
            {
                string userInput = searchTextBox.Text.Trim();
                if (string.IsNullOrEmpty(userInput))
                {
                    MessageBox.Show("请输入搜索内容", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                try
                {
                    string htmlContent = GetFuzzySearchResults(userInput);
                    newsBrowser.DocumentText = htmlContent;
                    searchForm.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"搜索时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            searchForm.Controls.Add(searchTextBox);
            searchForm.Controls.Add(searchButton);
            searchForm.ShowDialog();
        }

        private void categorySearchButton_Click(object sender, EventArgs e)
        {
            Form categoryForm = new Form
            {
                Text = "选择新闻类别",
                Size = new Size(400, 300),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            // 创建类别选择下拉框
            ComboBox categoryComboBox = new ComboBox
            {
                Location = new Point(20, 20),
                Size = new Size(340, 30),
                Font = new Font("Arial", 12),
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // 添加预定义的新闻类别
            categoryComboBox.Items.AddRange(new string[] { 
                "lifestyle",
                "health",
                "news",
                "sports",
                "entertainment",
                "autos",
                "foodanddrink",
                "travel",
                "video",
                "weather",
                "tv",
                "finance",
                "movies",
                "music",
            });

            Button searchButton = new Button
            {
                Text = "查询",
                Location = new Point(150, 70),
                Size = new Size(100, 40),
                Font = new Font("Arial", 12)
            };

            searchButton.Click += (s, ev) =>
            {
                if (categoryComboBox.SelectedItem == null)
                {
                    MessageBox.Show("请选择一个类别", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string selectedCategory = categoryComboBox.SelectedItem.ToString();
                try
                {
                    string htmlContent = GetCategoryNews(selectedCategory);
                    newsBrowser.DocumentText = htmlContent;
                    categoryForm.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"查询时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            categoryForm.Controls.Add(categoryComboBox);
            categoryForm.Controls.Add(searchButton);
            categoryForm.ShowDialog();
        }

        private string GetCategoryNews(string category)
        {
            string connStr = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=admin;Integrated Security=True";
            var htmlResults = new StringBuilder();

            htmlResults.Append("<html><head><style>");
            htmlResults.Append("body { font-family: Arial, sans-serif; margin: 0; padding: 10px; }");
            htmlResults.Append(".result-item { margin-bottom: 20px; padding: 10px; border-left: 4px solid #007bff; cursor: pointer; }");
            htmlResults.Append(".result-item:hover { background-color: #f8f9fa; }");
            htmlResults.Append(".title { font-size: 18px; color: #007bff; margin-bottom: 8px; }");
            htmlResults.Append(".summary { font-size: 14px; color: #666; line-height: 1.5; }");
            htmlResults.Append(".no-results { text-align: center; color: #666; padding: 20px; }");
            htmlResults.Append("</style></head><body>");

            using (SqlConnection connection = new SqlConnection(connStr))
            {
                connection.Open();
                string query = "SELECT TOP 10 newsid, title, abstract FROM news WHERE category = @category";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@category", category);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                string newsId = reader["newsid"].ToString();
                                string title = EscapeHtml(reader["title"].ToString());
                                string summary = EscapeHtml(reader["abstract"].ToString());

                                if (!string.IsNullOrEmpty(newsId) && !string.IsNullOrEmpty(title))
                                {
                                    htmlResults.Append($"<div class='result-item' onclick='window.external.ShowNewsDetail(\"{newsId}\", \"{title}\", \"{summary}\")'>");
                                    htmlResults.Append($"<div class='title'>{title}</div>");
                                    htmlResults.Append($"<div class='summary'>{summary}</div>");
                                    htmlResults.Append("</div>");
                                }
                            }
                        }
                        else
                        {
                            htmlResults.Append($"<div class='no-results'>未找到类别 '{category}' 的相关新闻</div>");
                        }
                    }
                }
            }

            htmlResults.Append("</body></html>");
            return htmlResults.ToString();
        }

        private void contentManagementButton_Click(object sender, EventArgs e)
        {
            Form managementForm = new Form
            {
                Text = "内容管理 - 添加新闻",
                Size = new Size(800, 600),
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MaximizeBox = false,
                MinimizeBox = false
            };

            // 创建输入控件
            var labels = new[] { "新闻ID:", "类别:", "子类别:", "标题:", "摘要:", "URL:", "英文标题:", "英文摘要:" };
            var textBoxes = new Dictionary<string, TextBox>();
            var categoryComboBox = new ComboBox();
            int yPos = 20;
            int labelWidth = 100;
            int textBoxWidth = 600;

            foreach (var label in labels)
            {
                var lbl = new Label
                {
                    Text = label,
                    Location = new Point(20, yPos),
                    Size = new Size(labelWidth, 20),
                    Font = new Font("Arial", 10)
                };

                if (label == "类别:")
                {
                    categoryComboBox = new ComboBox
                    {
                        Location = new Point(20 + labelWidth + 10, yPos),
                        Size = new Size(textBoxWidth, 20),
                        Font = new Font("Arial", 10),
                        DropDownStyle = ComboBoxStyle.DropDownList
                    };

                    // 添加预定义的新闻类别
                    categoryComboBox.Items.AddRange(new string[] { 
                        "lifestyle",
                        "health",
                        "news",
                        "sports",
                        "entertainment",
                        "autos",
                        "foodanddrink",
                        "travel",
                        "video",
                        "weather",
                        "tv",
                        "finance",
                        "movies",
                        "music"
                    });

                    managementForm.Controls.Add(lbl);
                    managementForm.Controls.Add(categoryComboBox);
                    yPos += categoryComboBox.Height + 10;
                }
                else
                {
                    var txt = new TextBox
                    {
                        Location = new Point(20 + labelWidth + 10, yPos),
                        Size = new Size(textBoxWidth, 20),
                        Font = new Font("Arial", 10)
                    };

                    // 对摘要字段使用多行文本框
                    if (label.Contains("摘要"))
                    {
                        txt.Multiline = true;
                        txt.Size = new Size(textBoxWidth, 80);
                        txt.ScrollBars = ScrollBars.Vertical;
                    }

                    managementForm.Controls.Add(lbl);
                    managementForm.Controls.Add(txt);
                    textBoxes[label] = txt;
                    yPos += txt.Height + 10;
                }
            }

            Button submitButton = new Button
            {
                Text = "提交",
                Location = new Point((managementForm.Width - 100) / 2, yPos + 20),
                Size = new Size(100, 30),
                Font = new Font("Arial", 12)
            };

            submitButton.Click += (s, ev) =>
            {
                try
                {
                    if (categoryComboBox.SelectedItem == null)
                    {
                        MessageBox.Show("请选择一个类别", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    string connStr = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=admin;Integrated Security=True";
                    using (SqlConnection connection = new SqlConnection(connStr))
                    {
                        connection.Open();
                        InsertRow(
                            connection,
                            "News",
                            textBoxes["新闻ID:"].Text,
                            categoryComboBox.SelectedItem.ToString(),
                            textBoxes["子类别:"].Text,
                            textBoxes["标题:"].Text,
                            textBoxes["摘要:"].Text,
                            textBoxes["URL:"].Text,
                            textBoxes["英文标题:"].Text,
                            textBoxes["英文摘要:"].Text
                        );
                        MessageBox.Show("新闻添加成功！", "成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        managementForm.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"添加新闻失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };

            managementForm.Controls.Add(submitButton);
            managementForm.ShowDialog();
        }

        private void InsertRow(SqlConnection connection, string tableName, string newsid, string category, string subcategory, string title, string abstractText, string url, string etitle, string eabstract)
        {
            using (SqlCommand cmd = connection.CreateCommand())
            {
                cmd.CommandText = $@"
                INSERT INTO [{tableName}] (newsid, category, subcategory, title, abstract, url, etitle, eabstract) 
                VALUES (@newsid, @category, @subcategory, @title, @abstract, @url, @etitle, @eabstract)";
                
                cmd.Parameters.AddWithValue("@newsid", (object)newsid ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@category", (object)category ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@subcategory", (object)subcategory ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@title", (object)title ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@abstract", (object)abstractText ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@url", (object)url ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@etitle", (object)etitle ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@eabstract", (object)eabstract ?? DBNull.Value);
                
                cmd.ExecuteNonQuery();
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {
        }
        private void InitializeComponent()
        {
            printDialog1 = new PrintDialog();
            SuspendLayout();
            // 
            // printDialog1
            // 
            printDialog1.UseEXDialog = true;
            // 
            // Form1
            // 
            ClientSize = new Size(500, 300);
            Name = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
        }

        private void StartHttpServer()
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://localhost:{port}/");
            httpListener.Start();
            isListening = true;

            Task.Run(async () =>
            {
                while (isListening)
                {
                    try
                    {
                        var context = await httpListener.GetContextAsync();
                        if (context.Request.Url.LocalPath == "/verify")
                        {
                            string token = context.Request.QueryString["token"];
                            if (!string.IsNullOrEmpty(token) && token == jwtValidationToken)
                            {
                                // 生成随机用户ID
                                Random random = new Random();
                                currentUserId = random.Next(0, 10000);
                                
                                // 在UI线程中更新界面
                                this.Invoke((MethodInvoker)delegate
                                {
                                    loginStatusLabel.Text = "登录成功！正在跳转...";
                                    loginStatusLabel.ForeColor = Color.Green;
                                    loginStatusLabel.Font = new Font("Microsoft YaHei", 12, FontStyle.Bold);
                                    loginStatusLabel.BackColor = Color.FromArgb(230, 245, 230);  // 浅绿色背景
                                    
                                    // 添加淡入淡出效果
                                    loginPanel.Visible = false;
                                    mainPanel.Visible = true;
                                    systemTitleLabel.Visible = false;
                                    mainPanel.BackColor = Color.FromArgb(0, mainPanel.BackColor);
                                    
                                    // 使用Timer实现淡入效果
                                    System.Windows.Forms.Timer fadeTimer = new System.Windows.Forms.Timer();
                                    fadeTimer.Interval = 20;
                                    fadeTimer.Tick += (s, e) =>
                                    {
                                        if (mainPanel.BackColor.A < 255)
                                        {
                                            mainPanel.BackColor = Color.FromArgb(
                                                Math.Min(255, mainPanel.BackColor.A + 25),
                                                mainPanel.BackColor
                                            );
                                        }
                                        else
                                        {
                                            fadeTimer.Stop();
                                            fadeTimer.Dispose();
                                        }
                                    };
                                    fadeTimer.Start();
                                });

                                // 发送成功响应页面
                                string successHtml = @"<html>
                                    <head>
                                        <meta charset='UTF-8'>
                                        <style>
                                            body { 
                                                font-family: Arial, sans-serif; 
                                                text-align: center; 
                                                margin: 0;
                                                padding: 0;
                                                background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
                                                height: 100vh;
                                                display: flex;
                                                align-items: center;
                                                justify-content: center;
                                            }
                                            .container {
                                                background: white;
                                                padding: 40px;
                                                border-radius: 10px;
                                                box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
                                                animation: fadeIn 0.5s ease-in-out;
                                            }
                                            .success { 
                                                color: #4CAF50; 
                                                font-size: 48px;
                                                margin-bottom: 20px;
                                            }
                                            .message { 
                                                color: #333;
                                                font-size: 18px;
                                                margin-top: 20px;
                                            }
                                            @keyframes fadeIn {
                                                from { opacity: 0; transform: translateY(-20px); }
                                                to { opacity: 1; transform: translateY(0); }
                                            }
                                        </style>
                                    </head>
                                    <body>
                                        <div class='container'>
                                            <div class='success'>✓</div>
                                            <div class='message'>Login successful! Please return to the application.</div>
                                        </div>
                                    </body>
                                </html>";
                                byte[] buffer = Encoding.UTF8.GetBytes(successHtml);
                                context.Response.ContentType = "text/html";
                                context.Response.ContentLength64 = buffer.Length;
                                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            }
                            else
                            {
                                // 在UI线程中更新界面
                                this.Invoke((MethodInvoker)delegate
                                {
                                    loginStatusLabel.Text = "登录失败，请重试！";
                                    loginStatusLabel.ForeColor = Color.Red;
                                    loginStatusLabel.Font = new Font("Microsoft YaHei", 12, FontStyle.Bold);
                                    loginStatusLabel.BackColor = Color.FromArgb(255, 230, 230);  // 浅红色背景
                                    
                                    // 添加抖动动画效果
                                    System.Windows.Forms.Timer shakeTimer = new System.Windows.Forms.Timer();
                                    int shakeCount = 0;
                                    int originalX = loginStatusLabel.Location.X;
                                    shakeTimer.Interval = 50;
                                    shakeTimer.Tick += (s, e) =>
                                    {
                                        if (shakeCount < 5)
                                        {
                                            loginStatusLabel.Location = new Point(
                                                originalX + (shakeCount % 2 == 0 ? 5 : -5),
                                                loginStatusLabel.Location.Y
                                            );
                                            shakeCount++;
                                        }
                                        else
                                        {
                                            loginStatusLabel.Location = new Point(originalX, loginStatusLabel.Location.Y);
                                            shakeTimer.Stop();
                                            shakeTimer.Dispose();
                                        }
                                    };
                                    shakeTimer.Start();
                                });

                                // 发送失败响应页面
                                string failureHtml = @"<html>
                                    <head>
                                        <meta charset='UTF-8'>
                                        <style>
                                            body { 
                                                font-family: Arial, sans-serif; 
                                                text-align: center; 
                                                margin: 0;
                                                padding: 0;
                                                background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
                                                height: 100vh;
                                                display: flex;
                                                align-items: center;
                                                justify-content: center;
                                            }
                                            .container {
                                                background: white;
                                                padding: 40px;
                                                border-radius: 10px;
                                                box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
                                                animation: shake 0.5s ease-in-out;
                                            }
                                            .error { 
                                                color: #f44336; 
                                                font-size: 48px;
                                                margin-bottom: 20px;
                                            }
                                            .message { 
                                                color: #333;
                                                font-size: 18px;
                                                margin-top: 20px;
                                            }
                                            @keyframes shake {
                                                0%, 100% { transform: translateX(0); }
                                                25% { transform: translateX(-10px); }
                                                75% { transform: translateX(10px); }
                                            }
                                        </style>
                                    </head>
                                    <body>
                                        <div class='container'>
                                            <div class='error'>✗</div>
                                            <div class='message'>Login failed! Please try again.</div>
                                        </div>
                                    </body>
                                </html>";
                                byte[] buffer = Encoding.UTF8.GetBytes(failureHtml);
                                context.Response.ContentType = "text/html";
                                context.Response.ContentLength64 = buffer.Length;
                                await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                            }
                        }
                        context.Response.Close();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"HTTP服务器错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            });
        }


        private async Task SendEmailAsync(string toEmail, string jwtToken)
        {
            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress("新闻推荐系统", SmtpUsername));
            emailMessage.To.Add(new MailboxAddress("", toEmail));
            emailMessage.Subject = "登录链接";
            emailMessage.Body = new TextPart("html")
            {
                Text = $"<html><body>" +
                       $"<p>请点击以下链接完成登录：</p>" +
                       $"<p><a href=\"http://localhost:{port}/verify?token={jwtToken}\">点击这里登录</a></p>" +
                       $"<p>如果链接无法点击，请复制以下地址到浏览器：</p>" +
                       $"<p>http://localhost:{port}/verify?token={jwtToken}</p>" +
                       $"<p>请在15分钟内完成登录。</p>" +
                       $"</body></html>"
            };

            using (var client = new SmtpClient())
            {
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(SmtpUsername, SmtpPassword);
                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
        }

        private async void sendLoginLinkButton_Click(object sender, EventArgs e)
        {
            string email = emailInputTextBox.Text.Trim();

            if (string.IsNullOrEmpty(email))
            {
                MessageBox.Show("邮箱不能为空！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string jwtToken = GenerateJwtToken(email);
            jwtValidationToken = jwtToken;

            try
            {
                await SendEmailAsync(email, jwtToken);
                loginStatusLabel.Text = "登录链接已发送，请检查邮箱并点击链接完成登录...";
                loginStatusLabel.ForeColor = Color.Blue;
                loginStatusLabel.BackColor = Color.FromArgb(230, 240, 255);  // 浅蓝色背景
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送邮件时发生错误: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GenerateJwtToken(string email)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(JwtSecret);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Email, email),
                    new Claim(ClaimTypes.Name, email)
                }),
                Expires = DateTime.UtcNow.AddMinutes(15),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private void logoutButton_Click(object sender, EventArgs e)
        {
            // 重置用户ID
            currentUserId = -1;
            
            // 显示登录面板，隐藏主面板
            loginPanel.Visible = true;
            mainPanel.Visible = false;
            systemTitleLabel.Visible = true;
            
            // 清空邮箱输入框和状态标签
            emailInputTextBox.Text = "";
            loginStatusLabel.Text = "等待登录...";
            loginStatusLabel.ForeColor = Color.Gray;
            loginStatusLabel.BackColor = Color.FromArgb(245, 245, 245);  // 恢复默认背景色
            loginStatusLabel.Font = new Font("Microsoft YaHei", 12);  // 恢复默认字体
        }

        private void systemUpdateButton_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show(
                "请更新数据库与个性推荐",
                "系统更新",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            
            if (result == DialogResult.OK)
            {
                Application.Exit();
            }
        }
    }
}