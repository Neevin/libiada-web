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
    
    public partial class CharacteristicType
    {
        public CharacteristicType()
        {
            this.BinaryCharacteristic = new HashSet<BinaryCharacteristic>();
            this.Characteristic = new HashSet<Characteristic>();
            this.CongenericCharacteristic = new HashSet<CongenericCharacteristic>();
        }
    
        public int Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public Nullable<int> CharacteristicGroupId { get; set; }
        public string ClassName { get; set; }
        public bool Linkable { get; set; }
        public bool FullSequenceApplicable { get; set; }
        public bool CongenericSequenceApplicable { get; set; }
        public bool BinarySequenceApplicable { get; set; }
    
        public virtual ICollection<BinaryCharacteristic> BinaryCharacteristic { get; set; }
        public virtual ICollection<Characteristic> Characteristic { get; set; }
        public virtual CharacteristicGroup CharacteristicGroup { get; set; }
        public virtual ICollection<CongenericCharacteristic> CongenericCharacteristic { get; set; }
    }
}
