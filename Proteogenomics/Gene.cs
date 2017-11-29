﻿using Bio;
using Proteomics;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace Proteogenomics
{
    public class Gene
    {

        #region Private Properties

        private MetadataListItem<List<string>> Metadata { get; set; }

        #endregion

        #region Public Properties

        public string ID { get; set; }

        public string ChromID { get; set; }

        public ISequence Chromosome { get; set; }

        public List<Transcript> Transcripts { get; set; } = new List<Transcript>();

        public HashSet<Exon> Exons { get; set; } = new HashSet<Exon>();

        public HashSet<Exon> CodingDomainSequences { get; set; } = new HashSet<Exon>();

        #endregion

        #region Public Constructors

        public Gene(string ID, ISequence chromosome, MetadataListItem<List<string>> metadata)
        {
            this.ID = ID;
            this.ChromID = chromosome.ID;
            this.Chromosome = chromosome;
            this.Metadata = metadata;
        }

        #endregion Public Constructors

        #region Public Methods

        public List<Protein> Translate(bool translateCodingDomains, bool includeVariants)
        {
            List<Protein> proteins = new List<Protein>();
            Parallel.ForEach(Transcripts, t =>
            {
                IEnumerable<Protein> p = t.Translate(translateCodingDomains, includeVariants);
                if (p != null)
                    lock (proteins) proteins.AddRange(p);
            });
            return proteins;
        }

        public List<Protein> TranslateUsingAnnotatedStartCodons(GeneModel genesWithCodingDomainSequences, bool includeVariants, int min_length)
        {
            int indexBinSize = 100000;
            Dictionary<Tuple<string, string, long>, List<Exon>> binnedCodingStarts = new Dictionary<Tuple<string, string, long>, List<Exon>>();
            foreach (Exon x in genesWithCodingDomainSequences.Genes.SelectMany(g => g.Transcripts).Select(t => t.CodingDomainSequences.FirstOrDefault()).Where(x => x != null))
            {
                Tuple<string, string, long> key = new Tuple<string, string, long>(x.ChromID, x.Strand, x.OneBasedStart / indexBinSize * indexBinSize);
                binnedCodingStarts.TryGetValue(key, out List<Exon> xs);
                if (xs == null) binnedCodingStarts.Add(key, new List<Exon> { x });
                else binnedCodingStarts[key].Add(x);
            }

            List<Protein> proteins = new List<Protein>();
            Parallel.ForEach(Transcripts, t =>
            {
                IEnumerable<Protein> p = t.TranslateUsingAnnotatedStartCodons(binnedCodingStarts, indexBinSize, min_length, includeVariants);
                if (p != null)
                    lock (proteins) proteins.AddRange(p);
            });
            return proteins;
        }

        #endregion Public Methods

    }
}