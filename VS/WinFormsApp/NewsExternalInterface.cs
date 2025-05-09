using System;
using System.Runtime.InteropServices;

namespace WinFormsApp1
{
    [ComVisible(true)]
    public class NewsExternalInterface
    {
        private Form1 mainForm;

        public NewsExternalInterface(Form1 mainForm)
        {
            this.mainForm = mainForm;
        }

        public void ShowNewsDetail(string newsId, string title, string content)
        {
            var newsDetailForm = new NewsDetailForm(title, content);
            newsDetailForm.Show();
        }
    }
} 