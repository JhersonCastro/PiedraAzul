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

        public string EntityType { get; set; } = default!; // Guest | Registered
    }

    public class PatientAutocompleteService : BaseLuceneService, IPatientAutocompleteService
    {
        public PatientAutocompleteService(string path)
            : base(path)
        {
        }

        // =========================
        // 👤 REGISTERED PATIENT
        // =========================
        public async Task IndexPatientAsync(PatientProfile patient)
        {
            if (patient == null || string.IsNullOrWhiteSpace(patient.UserId))
                return;

            var doc = new Document
            {
                // 🔥 CAMBIO CLAVE: ahora usamos UserId
                new StringField("Id", patient.UserId, Field.Store.YES),

                new TextField("Name", patient.User?.Name ?? "", Field.Store.YES),
                new TextField("Identification", patient.User?.IdentificationNumber ?? "", Field.Store.YES),
                new TextField("Phone", patient.User?.PhoneNumber ?? "", Field.Store.YES),

                new StringField("EntityType", "Registered", Field.Store.YES)
            };

            Writer.UpdateDocument(new Term("Id", patient.UserId), doc);
            Writer.Commit();
        }

        // =========================
        // 🧍 GUEST PATIENT
        // =========================
        public async Task IndexGuestAsync(PatientGuest guest)
        {
            if (guest == null || string.IsNullOrWhiteSpace(guest.PatientIdentification))
                return;

            var doc = new Document
            {
                new StringField("Id", guest.PatientIdentification, Field.Store.YES),

                new TextField("Name", guest.PatientName ?? "", Field.Store.YES),
                new TextField("Identification", guest.PatientIdentification ?? "", Field.Store.YES),
                new TextField("Phone", guest.PatientPhone ?? "", Field.Store.YES),

                new StringField("EntityType", "Guest", Field.Store.YES)
            };

            Writer.UpdateDocument(new Term("Id", guest.PatientIdentification), doc);
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
