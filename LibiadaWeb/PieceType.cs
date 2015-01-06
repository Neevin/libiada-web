//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LibiadaWeb
{
    using System;
    using System.Collections.Generic;
    
    public partial class PieceType
    {
        public PieceType()
        {
            this.Sequence = new HashSet<CommonSequence>();
            this.DnaSequence = new HashSet<DnaSequence>();
            this.LiteratureSequence = new HashSet<LiteratureSequence>();
            this.Fmotiv = new HashSet<Fmotiv>();
            this.Measure = new HashSet<Measure>();
            this.MusicSequence = new HashSet<MusicSequence>();
            this.Gene = new HashSet<Gene>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int NatureId { get; set; }
    
        public virtual ICollection<CommonSequence> Sequence { get; set; }
        public virtual ICollection<DnaSequence> DnaSequence { get; set; }
        public virtual ICollection<LiteratureSequence> LiteratureSequence { get; set; }
        public virtual Nature Nature { get; set; }
        public virtual ICollection<Fmotiv> Fmotiv { get; set; }
        public virtual ICollection<Measure> Measure { get; set; }
        public virtual ICollection<MusicSequence> MusicSequence { get; set; }
        public virtual ICollection<Gene> Gene { get; set; }
    }
}
