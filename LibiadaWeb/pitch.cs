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
    
    public partial class Pitch
    {
        public int Id { get; set; }
        public int Octave { get; set; }
        public int Midinumber { get; set; }
        public int InstrumentId { get; set; }
        public long NoteId { get; set; }
        public int AccidentalId { get; set; }
        public int NoteSymbolId { get; set; }
        public System.DateTimeOffset Created { get; set; }
        public Nullable<System.DateTimeOffset> Modified { get; set; }
    
        public virtual Note Note { get; set; }
        public virtual Instrument Instrument { get; set; }
        public virtual Accidental Accidental { get; set; }
        public virtual NoteSymbol NoteSymbol { get; set; }
    }
}
