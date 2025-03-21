using LLMDemos.Models;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using System.ComponentModel;

namespace LLMDemos.Plugins
{
    class VectorDataSearcher<TKey> where TKey : notnull
    {
        private readonly IVectorStoreRecordCollection<TKey, TextSnippet<TKey>> _vectorStoreRecordCollection;
        private readonly IEmbeddingGenerator<string, Embedding<float>> _embeddingGenerator;

        public VectorDataSearcher(IVectorStoreRecordCollection<TKey, TextSnippet<TKey>> vectorStoreRecordCollection, IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator)
        {
            _vectorStoreRecordCollection = vectorStoreRecordCollection;
            _embeddingGenerator = embeddingGenerator;
        }

        /// <summary>
        /// 从向量存储中获取用户查询的前N个文本搜索结果（默认N为1）
        /// </summary>
        /// <param name="query"></param>
        /// <param name="topN"></param>
        /// <returns>文本搜索结果集合</returns>
        //[Description("Get top N text search results from vector store by user's query (N is 1 by default)")]
        //[return: Description("Collection of text search result")]
        [KernelFunction("Results_from_vectorstore")]
        [Description("Collection of text search result")]
        public async Task<IEnumerable<TextSearchResult>> GetTextSearchResults(string query, int topN = 1)
        {
            var defaultForegroundColor = Console.ForegroundColor;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write($"（{nameof(GetTextSearchResults)}：正在检索向量数据库...{query}，{topN}）");

            var queryEmbedding = await _embeddingGenerator.GenerateEmbeddingVectorAsync(query);
            // Query from vector data store
            var searchOptions = new VectorSearchOptions<TextSnippet<TKey>>()
            {
                Top = topN,
                //VectorPropertyName = nameof(TextSnippet<TKey>.TextEmbedding)
            };
            var searchResults = await _vectorStoreRecordCollection.VectorizedSearchAsync(queryEmbedding, searchOptions);
            var responseResults = new List<TextSearchResult>();
            await foreach (var result in searchResults.Results)
            {
                responseResults.Add(new TextSearchResult()
                {
                    Value = result.Record.Text ?? string.Empty,
                    Link = result.Record.ReferenceLink ?? string.Empty,
                    Score = result.Score
                });
            }

            Console.WriteLine($"（{nameof(GetTextSearchResults)}：{JsonConvert.SerializeObject(responseResults)}）");

            Console.ForegroundColor = defaultForegroundColor;

            return responseResults;
        }
    }
}
