using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MuhasebeAPI.Models
{
    // DB entity
    public class GelirGider
    {
        public int Id { get; set; }
        public DateTime Tarih { get; set; }
        public string Turu { get; set; }
        public int KategoriID { get; set; }
        public string Aciklama { get; set; }
        public decimal Tutar { get; set; }
    }

   

}
