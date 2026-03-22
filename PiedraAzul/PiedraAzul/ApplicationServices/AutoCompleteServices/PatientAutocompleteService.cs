using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using PiedraAzul.Data.Models;

namespace PiedraAzul.ApplicationServices.AutoCompleteServices
{
    public interface IPatientAutocompleteService
    {
        void IndexPatient(object patient);
        List<PatientAutocompleteResult> Search(string text, int max = 10);
    }

    public class PatientAutocompleteResult
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Identification { get; set; }
        public string? Phone { get; set; }
        public string EntityType { get; set; } = default!;
    }

    public class PatientAutocompleteService : IPatientAutocompleteService, IDisposable
    {
        private readonly Analyzer _analyzer;
        private readonly IndexWriter _writer;
        private readonly SearcherManager _searcherManager;

        private static readonly LuceneVersion AppLuceneVersion = LuceneVersion.LUCENE_48;

        public PatientAutocompleteService(string indexPath)
        {
            if (!System.IO.Directory.Exists(indexPath))
                System.IO.Directory.CreateDirectory(indexPath);

            var dir = FSDirectory.Open(indexPath);

            if (IndexWriter.IsLocked(dir))
            {
                IndexWriter.Unlock(dir);
            }

            _analyzer = new StandardAnalyzer(AppLuceneVersion);

            var config = new IndexWriterConfig(AppLuceneVersion, _analyzer);

            _writer = new IndexWriter(dir, config);

            _searcherManager = new SearcherManager(_writer, true, null);

            Console.WriteLine("✅ Lucene inicializado correctamente");
        }

        // =========================
        // 🔹 INDEXAR
        // =========================
        public void IndexPatient(object patient)
        {
            Document doc;

            if (patient is PatientGuest guest)
            {
                doc = new Document
                {
                    new StringField("Id", guest.PatientIdentification, Field.Store.YES),
                    new TextField("Name", guest.PatientName ?? "", Field.Store.YES),
                    new TextField("Identification", guest.PatientIdentification ?? "", Field.Store.YES),
                    new TextField("Phone", guest.PatientPhone ?? "", Field.Store.YES),
                    new StringField("EntityType", "Guest", Field.Store.YES)
                };
            }
            else if (patient is PatientProfile profile)
            {
                doc = new Document
                {
                    new StringField("Id", profile.PatientId.ToString(), Field.Store.YES),
                    new TextField("Name", profile.User?.Name ?? "", Field.Store.YES),
                    new TextField("Identification", profile.User?.IdentificationNumber ?? "", Field.Store.YES),
                    new TextField("Phone", profile.User?.PhoneNumber ?? "", Field.Store.YES),
                    new StringField("EntityType", "Profile", Field.Store.YES)
                };
            }
            else
            {
                throw new ArgumentException("Tipo no soportado");
            }

            var id = doc.Get("Id");

            _writer.UpdateDocument(new Term("Id", id), doc);
            _writer.Commit();
        }

        // =========================
        // 🔍 BUSCAR
        // =========================
        public List<PatientAutocompleteResult> Search(string text, int max = 10)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<PatientAutocompleteResult>();

            _searcherManager.MaybeRefreshBlocking();
            var searcher = _searcherManager.Acquire();

            try
            {
                var fields = new[] { "Name", "Identification", "Phone" };

                var parser = new MultiFieldQueryParser(AppLuceneVersion, fields, _analyzer);

                var queryText = BuildPrefixQuery(text);
                var query = parser.Parse(queryText);

                var hits = searcher.Search(query, max).ScoreDocs;

                var results = new List<PatientAutocompleteResult>();

                foreach (var hit in hits)
                {
                    var doc = searcher.Doc(hit.Doc);

                    results.Add(new PatientAutocompleteResult
                    {
                        Id = doc.Get("Id"),
                        Name = doc.Get("Name"),
                        Identification = doc.Get("Identification"),
                        Phone = doc.Get("Phone"),
                        EntityType = doc.Get("EntityType")
                    });
                }

                return results;
            }
            finally
            {
                _searcherManager.Release(searcher);
            }
        }

        private string BuildPrefixQuery(string text)
        {
            var escaped = QueryParserBase.Escape(text);
            var terms = escaped.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (terms.Length == 0)
                return "*";

            return string.Join(" AND ", terms.Select(t => t + "*"));
        }

        public void Dispose()
        {
            _searcherManager?.Dispose();
            _writer?.Dispose();
            _analyzer?.Dispose();
        }
    }
}