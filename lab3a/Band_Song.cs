//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace lab3a
{
    using System;
    using System.Collections.Generic;
    
    public partial class Band_Song
    {
        public int Id { get; set; }
        public int Band_id { get; set; }
        public int Song_id { get; set; }
    
        public virtual Band Band { get; set; }
        public virtual Song Song { get; set; }
    }
}
