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
    
    public partial class CongenericCharacteristicValue
    {
        public long Id { get; set; }
        public long SequenceId { get; set; }
        public short CharacteristicTypeLinkId { get; set; }
        public double Value { get; set; }
        public System.DateTimeOffset Created { get; set; }
        public long ElementId { get; set; }
        public System.DateTimeOffset Modified { get; set; }
    
        public virtual CongenericCharacteristicLink CongenericCharacteristicLink { get; set; }
        public virtual Element Element { get; set; }
    }
}
