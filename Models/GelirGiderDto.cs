namespace MuhasebeAPI.Models
{
    // GET DTO
    public class GelirGiderDto
    {
        public int Id { get; set; }
        public DateTime Tarih { get; set; }
        public string Turu { get; set; }
        public int KategoriID { get; set; }
        public string Aciklama { get; set; }
        public decimal Tutar { get; set; }
        public string KategoriAdi { get; set; }
    }
}
