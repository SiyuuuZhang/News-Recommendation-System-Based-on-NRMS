using System;
using System.Windows.Forms;
using System.Drawing;

namespace WinFormsApp1
{
    public class NewsDetailForm : Form
    {
        private WebBrowser newsContentBrowser;
        private Button backToListButton;

        public NewsDetailForm(string title, string content)
        {
            this.Text = title;
            this.Size = new Size(1200, 800);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.MinimumSize = new Size(1000, 700);

            // 创建WebBrowser
            newsContentBrowser = new WebBrowser
            {
                Dock = DockStyle.Fill,
                ScriptErrorsSuppressed = true
            };

            // 创建返回按钮
            backToListButton = new Button
            {
                Text = "返回列表",
                Size = new Size(120, 40),
                Font = new Font("Microsoft YaHei", 12),
                BackColor = Color.FromArgb(0, 123, 255),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            backToListButton.FlatAppearance.BorderSize = 0;
            backToListButton.Click += (s, e) => this.Close();

            // 创建面板来容纳WebBrowser和按钮
            Panel mainContentPanel = new Panel
            {
                Dock = DockStyle.Fill
            };

            // 创建按钮面板
            Panel buttonContainerPanel = new Panel
            {
                Height = 60,
                Dock = DockStyle.Bottom,
                Padding = new Padding(10)
            };

            // 将按钮添加到按钮面板
            buttonContainerPanel.Controls.Add(backToListButton);
            backToListButton.Dock = DockStyle.None;
            backToListButton.Anchor = AnchorStyles.None;

            // 将WebBrowser添加到主面板
            mainContentPanel.Controls.Add(newsContentBrowser);
            mainContentPanel.Controls.Add(buttonContainerPanel);

            // 将主面板添加到窗体
            this.Controls.Add(mainContentPanel);

            // 构建HTML内容
            string htmlContent = $@"
                <html>
                <head>
                    <style>
                        body {{ 
                            font-family: 'Microsoft YaHei', Arial, sans-serif;
                            margin: 0;
                            padding: 0;
                            background-color: #f5f5f5;
                            color: #333;
                        }}
                        .container {{
                            max-width: 800px;
                            margin: 0 auto;
                            padding: 20px;
                            background-color: white;
                            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                            min-height: calc(100vh - 100px);
                            margin-bottom: 80px;
                            position: relative;
                        }}
                        .title {{
                            font-size: 28px;
                            color: #1a1a1a;
                            margin-bottom: 20px;
                            padding-bottom: 15px;
                            border-bottom: 2px solid #007bff;
                            line-height: 1.4;
                        }}
                        .content {{
                            font-size: 16px;
                            color: #444;
                            line-height: 1.8;
                            text-align: justify;
                            margin-bottom: 60px;
                        }}
                        .meta {{
                            color: #666;
                            font-size: 14px;
                            margin-bottom: 20px;
                            padding-bottom: 15px;
                            border-bottom: 1px solid #eee;
                        }}
                        .meta span {{
                            margin-right: 20px;
                        }}
                        .meta i {{
                            color: #007bff;
                            margin-right: 5px;
                        }}
                        .tags {{
                            margin-top: 20px;
                            margin-bottom: 60px;
                        }}
                        .tag {{
                            display: inline-block;
                            padding: 4px 12px;
                            background-color: #e9ecef;
                            color: #495057;
                            border-radius: 15px;
                            margin-right: 10px;
                            margin-bottom: 10px;
                            font-size: 14px;
                        }}
                        .tag:hover {{
                            background-color: #007bff;
                            color: white;
                            cursor: pointer;
                        }}
                        @media (max-width: 768px) {{
                            .container {{
                                padding: 15px;
                            }}
                            .title {{
                                font-size: 24px;
                            }}
                            .content {{
                                font-size: 15px;
                            }}
                        }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='title'>{title}</div>
                        <div class='meta'>
                            <span><i>📅</i>发布时间：{DateTime.Now.ToString("yyyy-MM-dd HH:mm")}</span>
                            <span><i>👁️</i>阅读量：{new Random().Next(100, 1000)}</span>
                        </div>
                        <div class='content'>{content}</div>
                        <div class='tags'>
                            <span class='tag'>新闻</span>
                            <span class='tag'>热点</span>
                            <span class='tag'>资讯</span>
                        </div>
                    </div>
                </body>
                </html>";

            newsContentBrowser.DocumentText = htmlContent;

            // 在窗体加载完成后调整按钮位置
            this.Load += (s, e) =>
            {
                // 计算按钮位置
                int buttonX = (buttonContainerPanel.Width - backToListButton.Width) / 2;
                int buttonY = (buttonContainerPanel.Height - backToListButton.Height) / 2;
                backToListButton.Location = new Point(buttonX, buttonY);
            };
        }
    }
} 