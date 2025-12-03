
using Microsoft.AspNetCore.Mvc;
using MuhasebeAPI.Helpers;
using MuhasebeAPI.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace MuhasebeAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KategorilerController : ControllerBase
    {
        private readonly Baglanti _baglanti;

        public KategorilerController(Baglanti baglanti)
        {
            _baglanti = baglanti;
        }

        // 📌 1️⃣ Tüm kategorileri listele
        [HttpGet]
        public IActionResult GetKategoriler()
        {
            List<Kategori> kategoriler = new List<Kategori>();

            using (SqlConnection conn = _baglanti.GetConnection())
            {
                conn.Open();
                string query = "SELECT KategoriID, KategoriAdi, Tur FROM Kategoriler";
                SqlCommand cmd = new SqlCommand(query, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    kategoriler.Add(new Kategori
                    {
                        KategoriID = Convert.ToInt32(reader["KategoriID"]),
                        KategoriAdi = reader["KategoriAdi"].ToString(),
                        Tur = reader["Tur"].ToString()
                    });
                }
            }

            return Ok(kategoriler);
        }

        // 📌 2️⃣ Yeni kategori ekle
        [HttpPost]
        public IActionResult AddKategori([FromBody] Kategori kategori)
        {
            if (kategori == null || string.IsNullOrWhiteSpace(kategori.KategoriAdi))
                return BadRequest("Kategori adı boş olamaz.");

            using (SqlConnection conn = _baglanti.GetConnection())
            {
                conn.Open();
                string query = "INSERT INTO Kategoriler (KategoriAdi, Tur) VALUES (@adi, @tur)";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@adi", kategori.KategoriAdi);
                cmd.Parameters.AddWithValue("@tur", kategori.Tur ?? "");
                cmd.ExecuteNonQuery();
            }

            return Ok("Kategori başarıyla eklendi.");
        }

        // 📌 3️⃣ Kategori güncelle
        [HttpPut("{id}")]
        public IActionResult UpdateKategori(int id, [FromBody] Kategori kategori)
        {
            if (kategori == null || string.IsNullOrWhiteSpace(kategori.KategoriAdi))
                return BadRequest("Kategori adı boş olamaz.");

            using (SqlConnection conn = _baglanti.GetConnection())
            {
                conn.Open();
                string query = "UPDATE Kategoriler SET KategoriAdi=@adi, Tur=@tur WHERE KategoriID=@id";
                SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@adi", kategori.KategoriAdi);
                cmd.Parameters.AddWithValue("@tur", kategori.Tur ?? "");
                cmd.Parameters.AddWithValue("@id", id);

                int rows = cmd.ExecuteNonQuery();
                if (rows == 0)
                    return NotFound("Kategori bulunamadı.");
            }

            return Ok("Kategori güncellendi.");
        }

        // 📌 4️⃣ Kategori sil
        [HttpDelete("{id}")]
        public IActionResult DeleteKategori(int id)
        {
            try
            {
                using (SqlConnection conn = _baglanti.GetConnection())
                {
                    conn.Open();
                    string query = "DELETE FROM Kategoriler WHERE KategoriID=@id";
                    SqlCommand cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", id);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                        return NotFound("Kategori bulunamadı.");
                }

                return Ok("Kategori silindi.");
            }
            catch (Exception)
            {

                return NotFound("Bu kategoriye ait kayıt eklenmiş silinemez!");
            }
           
        }
    }
}
