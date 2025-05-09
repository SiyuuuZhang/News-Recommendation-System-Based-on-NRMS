using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;

namespace TsvToDatabaseImporter
{
    class Program
    {
        private const string DatabaseConnectionString = @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=admin;Integrated Security=True";

        static async Task Main(string[] args)
        {
            // TSV 文件导入
            string tsvFilePath = @"F:\VSCSworkspack\News-recommendation-system-view\NRMS\data\dev_news.tsv";
            string newsTableName = "news";
            await ImportTsvToDatabaseAsync(tsvFilePath, newsTableName);

            // CSV 文件导入
            string csvFilePath = @"F:\VSCSworkspack\News-recommendation-system-view\recommendations.csv";
            await ImportCsvToDatabaseAsync(csvFilePath);

            Console.WriteLine("数据导入完成");
        }

        static async Task ImportTsvToDatabaseAsync(string filePath, string tableName)
        {
            string[] fileLines = File.ReadAllLines(filePath);
            if (fileLines.Length == 0)
            {
                Console.WriteLine("错误：TSV文件为空");
                return;
            }

            using (SqlConnection dbConnection = new SqlConnection(DatabaseConnectionString))
            {
                await dbConnection.OpenAsync();
                for (int lineIndex = 0; lineIndex < fileLines.Length; lineIndex++)
                {
                    string[] fields = fileLines[lineIndex].Split(new[] { '\t' }, StringSplitOptions.None);
                    if (fields.Length < 8)
                    {
                        Console.WriteLine($"警告：第 {lineIndex} 行字段数量不足: {fields.Length}");
                        continue;
                    }

                    // 去除每个字段中的双引号
                    string newsId = fields.Length > 0 ? fields[0].Replace("\"", "") : null;
                    string category = fields.Length > 1 ? fields[1].Replace("\"", "") : null;
                    string subCategory = fields.Length > 2 ? fields[2].Replace("\"", "") : null;
                    string title = fields.Length > 3 ? fields[3].Replace("\"", "") : null;
                    string abstractText = fields.Length > 4 ? fields[4].Replace("\"", "") : null;
                    string url = fields.Length > 5 ? fields[5].Replace("\"", "") : null;
                    string englishTitle = fields.Length > 6 ? fields[6].Replace("\"", "") : null;
                    string englishAbstract = fields.Length > 7 ? fields[7].Replace("\"", "") : null;

                    await InsertNewsRowAsync(dbConnection, tableName, newsId, category, subCategory, title, abstractText, url, englishTitle, englishAbstract);
                }
            }
            Console.WriteLine("TSV数据导入完成");
        }

        static async Task ImportCsvToDatabaseAsync(string filePath)
        {
            using (var dbConnection = new SqlConnection(DatabaseConnectionString))
            {
                await dbConnection.OpenAsync();

                using (var fileReader = new StreamReader(filePath))
                {
                    string currentLine;
                    bool isHeaderLine = true;

                    while ((currentLine = await fileReader.ReadLineAsync()) != null)
                    {
                        if (isHeaderLine)
                        {
                            isHeaderLine = false;
                            continue;
                        }

                        string[] values = currentLine.Split(',');

                        if (values.Length >= 2)
                        {
                            string userId = values[0].Trim();
                            string impression = values[1].Trim();

                            await InsertUserImpressionAsync(dbConnection, userId, impression);
                        }
                    }
                }
            }
            Console.WriteLine("CSV数据导入完成");
        }

        static async Task InsertNewsRowAsync(SqlConnection connection, string tableName, string newsId, string category, string subCategory, string title, string abstractText, string url, string englishTitle, string englishAbstract)
        {
            using (SqlCommand sqlCommand = connection.CreateCommand())
            {
                sqlCommand.CommandText = $@"
                INSERT INTO [{tableName}] (newsid, category, subcategory, title, abstract, url, etitle, eabstract) 
                VALUES (@newsid, @category, @subcategory, @title, @abstract, @url, @etitle, @eabstract)";

                sqlCommand.Parameters.AddWithValue("@newsid", (object)newsId ?? DBNull.Value);
                sqlCommand.Parameters.AddWithValue("@category", (object)category ?? DBNull.Value);
                sqlCommand.Parameters.AddWithValue("@subcategory", (object)subCategory ?? DBNull.Value);
                sqlCommand.Parameters.AddWithValue("@title", (object)title ?? DBNull.Value);
                sqlCommand.Parameters.AddWithValue("@abstract", (object)abstractText ?? DBNull.Value);
                sqlCommand.Parameters.AddWithValue("@url", (object)url ?? DBNull.Value);
                sqlCommand.Parameters.AddWithValue("@etitle", (object)englishTitle ?? DBNull.Value);
                sqlCommand.Parameters.AddWithValue("@eabstract", (object)englishAbstract ?? DBNull.Value);

                await sqlCommand.ExecuteNonQueryAsync();
            }
        }

        static async Task InsertUserImpressionAsync(SqlConnection connection, string userId, string impression)
        {
            using (var sqlCommand = connection.CreateCommand())
            {
                sqlCommand.CommandText = "INSERT INTO UserImpressions (UserId, Impression) VALUES (@UserId, @Impression)";
                sqlCommand.Parameters.AddWithValue("@UserId", userId);
                sqlCommand.Parameters.AddWithValue("@Impression", impression);

                await sqlCommand.ExecuteNonQueryAsync();
            }
        }
    }
}