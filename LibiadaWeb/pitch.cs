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
    
    public partial class pitch
    {
        public int id { get; set; }
        public int octave { get; set; }
        public int midinumber { get; set; }
        public int instrument_id { get; set; }
        public long note_id { get; set; }
        public int accidental_id { get; set; }
        public int note_symbol_id { get; set; }
        public System.DateTimeOffset created { get; set; }
        public Nullable<System.DateTimeOffset> modified { get; set; }
    
        public virtual note note { get; set; }
        public virtual instrument instrument { get; set; }
        public virtual accidental accidental { get; set; }
        public virtual note_symbol note_symbol { get; set; }
    }
}