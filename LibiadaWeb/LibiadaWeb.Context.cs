﻿//------------------------------------------------------------------------------
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
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    
    public partial class LibiadaWebEntities : DbContext
    {
        public LibiadaWebEntities()
            : base("name=LibiadaWebEntities")
        {
    		Database.CommandTimeout = 1000000;
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public virtual DbSet<AccordanceCharacteristicValue> AccordanceCharacteristicValue { get; set; }
        public virtual DbSet<BinaryCharacteristicValue> BinaryCharacteristicValue { get; set; }
        public virtual DbSet<CommonSequence> CommonSequence { get; set; }
        public virtual DbSet<CharacteristicValue> CharacteristicValue { get; set; }
        public virtual DbSet<DnaSequence> DnaSequence { get; set; }
        public virtual DbSet<Subsequence> Subsequence { get; set; }
        public virtual DbSet<Position> Position { get; set; }
        public virtual DbSet<Element> Element { get; set; }
        public virtual DbSet<LiteratureSequence> LiteratureSequence { get; set; }
        public virtual DbSet<Matter> Matter { get; set; }
        public virtual DbSet<SequenceAttribute> SequenceAttribute { get; set; }
        public virtual DbSet<Fmotif> Fmotif { get; set; }
        public virtual DbSet<CongenericCharacteristicValue> CongenericCharacteristicValue { get; set; }
        public virtual DbSet<Measure> Measure { get; set; }
        public virtual DbSet<MusicSequence> MusicSequence { get; set; }
        public virtual DbSet<DataSequence> DataSequence { get; set; }
        public virtual DbSet<Note> Note { get; set; }
        public virtual DbSet<Pitch> Pitch { get; set; }
        public virtual DbSet<AccordanceCharacteristicLink> AccordanceCharacteristicLink { get; set; }
        public virtual DbSet<BinaryCharacteristicLink> BinaryCharacteristicLink { get; set; }
        public virtual DbSet<FullCharacteristicLink> FullCharacteristicLink { get; set; }
        public virtual DbSet<CongenericCharacteristicLink> CongenericCharacteristicLink { get; set; }
        public virtual DbSet<AspNetRole> AspNetRoles { get; set; }
        public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }
        public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }
        public virtual DbSet<AspNetUser> AspNetUsers { get; set; }
        public virtual DbSet<CalculationTask> CalculationTask { get; set; }
        public virtual DbSet<SequenceGroup> SequenceGroup { get; set; }
    }
}
