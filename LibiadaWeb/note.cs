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
    
    public partial class Note
    {
        public Note()
        {
            Pitch = new HashSet<Pitch>();
        }
    
        public long Id { get; set; }
        public string Value { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public Notation Notation { get; set; }
        public System.DateTimeOffset Created { get; set; }
        public int Numerator { get; set; }
        public int Denominator { get; set; }
        public bool Triplet { get; set; }
        public LibiadaCore.Core.SimpleTypes.Tie Tie { get; set; }
        public System.DateTimeOffset Modified { get; set; }
    
        public virtual ICollection<Pitch> Pitch { get; set; }
    }
}
