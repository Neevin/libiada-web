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
    
    public partial class Piece
    {
        public long Id { get; set; }
        public long GeneId { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
    
        public virtual Gene Gene { get; set; }
    }
}
