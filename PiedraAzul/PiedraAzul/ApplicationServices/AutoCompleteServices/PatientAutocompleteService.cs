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
        Task IndexPatientAsync(PatientProfile patient);
        Task IndexGuestAsync(PatientGuest guest);

        Task<List<PatientAutocompleteResult>> SearchAsync(string text, int max = 10);
    }

    public class PatientAutocompleteResult
    {
        public string Id { get; set; } = default!;
        public string Name { get; set; } = default!;
        public string? Identification { get; set; }
        public string? Phone { get; set; }
        public string EntityType { get; set; } = default!; // Guest | Profile
    }

    public class PatientAutocompleteService : BaseLuceneService, IPatientAutocompleteService
    {
        public PatientAutocompleteService(string path)
            : base(path)
        {
        }

        // =========================
        // 👤 PROFILE
        // =========================
        public async Task IndexPatientAsync(PatientProfile patient)
        {
            var doc = new Document
        {
            new StringField("Id", patient.PatientId.ToString(), Field.Store.YES),
            new TextField("Name", patient.User?.Name ?? "", Field.Store.YES),
            new TextField("Identification", patient.User?.IdentificationNumber ?? "", Field.Store.YES),
            new TextField("Phone", patient.User?.PhoneNumber ?? "", Field.Store.YES),
            new StringField("EntityType", "Profile", Field.Store.YES)
        };

            Writer.UpdateDocument(new Term("Id", doc.Get("Id")), doc);
            Writer.Commit();
        }

        // =========================
        // 🧍 GUEST
        // =========================
        public async Task IndexGuestAsync(PatientGuest guest)
        {
            var doc = new Document
        {
            new StringField("Id", guest.PatientIdentification, Field.Store.YES),
            new TextField("Name", guest.PatientName ?? "", Field.Store.YES),
            new TextField("Identification", guest.PatientIdentification ?? "", Field.Store.YES),
            new TextField("Phone", guest.PatientPhone ?? "", Field.Store.YES),
            new StringField("EntityType", "Guest", Field.Store.YES)
        };

            Writer.UpdateDocument(new Term("Id", doc.Get("Id")), doc);
            Writer.Commit();
        }

        // =========================
        // 🔍 SEARCH
        // =========================
        public async Task<List<PatientAutocompleteResult>> SearchAsync(string text, int max = 10)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new();

            SearcherManager.MaybeRefreshBlocking();
            var searcher = SearcherManager.Acquire();

            try
            {
                var fields = new[] { "Name", "Identification", "Phone" };
                var parser = new MultiFieldQueryParser(AppLuceneVersion, fields, Analyzer);

                var query = parser.Parse(BuildPrefixQuery(text));
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
                SearcherManager.Release(searcher);
            }
        }
    }
    }
