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
    public class GelirGiderController : ControllerBase
    {
        private readonly Baglanti _baglanti;

        public GelirGiderController(Baglanti baglanti)
        {
            _baglanti = baglanti;
        }

        // 📌 1️⃣ Tüm kayıtları listele
        [HttpGet]
        public IActionResult GetGelirGiderListesi([FromQuery] string tur = null, [FromQuery] DateTime? baslangicTarihi = null, [FromQuery] DateTime? bitisTarihi = null)
        {
            List<GelirGiderDto> list = new List<GelirGiderDto>();

            using (SqlConnection conn = _baglanti.GetConnection())
            {
                conn.Open();
                string query = @"
                    SELECT 
                        g.Id, 
                        g.Tarih, 
                        g.Turu, 
                        g.KategoriID, 
                        k.KategoriAdi,
                        g.Aciklama, 
                        g.Tutar 
                    FROM GelirGider g
                    INNER JOIN Kategoriler k ON g.KategoriID = k.KategoriID
                    WHERE 1=1";

                if (!string.IsNullOrEmpty(tur))
                    query += " AND g.Turu = @Tur";

                if (baslangicTarihi.HasValue)
                    query += " AND g.Tarih >= @BaslangicTarihi";

                if (bitisTarihi.HasValue)
                    query += " AND g.Tarih <= @BitisTarihi";

                query += " ORDER BY g.Id DESC";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (!string.IsNullOrEmpty(tur))
                        cmd.Parameters.AddWithValue("@Tur", tur);

                    if (baslangicTarihi.HasValue)
                        cmd.Parameters.AddWithValue("@BaslangicTarihi", baslangicTarihi.Value);

                    if (bitisTarihi.HasValue)
                        cmd.Parameters.AddWithValue("@BitisTarihi", bitisTarihi.Value);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new GelirGiderDto
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Tarih = Convert.ToDateTime(reader["Tarih"]),
                                Turu = reader["Turu"].ToString(),
                                KategoriID = Convert.ToInt32(reader["KategoriID"]),
                                Aciklama = reader["Aciklama"].ToString(),
                                Tutar = Convert.ToDecimal(reader["Tutar"]),
                                KategoriAdi = reader["KategoriAdi"] == DBNull.Value ? "-" : reader["KategoriAdi"].ToString()
                            });


                        }
                    }
                }
            }

            return Ok(list);
        }

        // 📌 2️⃣ Yeni kayıt ekle
        [HttpPost]
        public IActionResult AddGelirGider([FromBody] GelirGider gelirGider)
        {
            if (gelirGider == null)
                return BadRequest("Kayıt bilgileri boş olamaz.");

            if (gelirGider.Tutar <= 0)
                return BadRequest("Tutar 0'dan büyük olmalıdır.");

            using (SqlConnection conn = _baglanti.GetConnection())
            {
                conn.Open();
                string query = @"
                    INSERT INTO GelirGider 
                    (Tarih, Turu, KategoriID, Aciklama, Tutar) 
                    VALUES 
                    (@Tarih, @Turu, @KategoriID, @Aciklama, @Tutar)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Tarih", gelirGider.Tarih);
                    cmd.Parameters.AddWithValue("@Turu", gelirGider.Turu);
                    cmd.Parameters.AddWithValue("@KategoriID", gelirGider.KategoriID);
                    cmd.Parameters.AddWithValue("@Aciklama", gelirGider.Aciklama ?? "");
                    cmd.Parameters.AddWithValue("@Tutar", gelirGider.Tutar);

                    cmd.ExecuteNonQuery();
                }
            }

            return Ok("Kayıt başarıyla eklendi.");
        }
        // 📌 Tek kayıt getir
        [HttpGet("{id}")]
        public IActionResult GetGelirGiderById(int id)
        {
            GelirGider gelirGider = null;

            using (SqlConnection conn = _baglanti.GetConnection())
            {
                conn.Open();
                string query = @"
            SELECT g.Id, g.Tarih, g.Turu, g.KategoriID, k.KategoriAdi, g.Aciklama, g.Tutar
            FROM GelirGider g
            LEFT JOIN Kategoriler k ON g.KategoriID = k.KategoriID
            WHERE g.Id=@Id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            gelirGider = new GelirGider
                            {
                                Id = Convert.ToInt32(reader["Id"]),
                                Tarih = Convert.ToDateTime(reader["Tarih"]),
                                Turu = reader["Turu"].ToString(),
                                KategoriID = Convert.ToInt32(reader["KategoriID"]),
                                Aciklama = reader["Aciklama"].ToString(),
                                Tutar = Convert.ToDecimal(reader["Tutar"])
                                // KategoriAdi sadece GET/DTO için, modalda seçmek için kategoriID yeterli
                            };
                        }
                    }
                }
            }

            if (gelirGider == null)
                return NotFound("Kayıt bulunamadı.");

            return Ok(gelirGider);
        }

        // 📌 3️⃣ Kayıt güncelle
        [HttpPut("{id}")]
        public IActionResult UpdateGelirGider(int id, [FromBody] GelirGider gelirGider)
        {
            if (gelirGider == null)
                return BadRequest("Kayıt bilgileri boş olamaz.");

            if (gelirGider.Tutar <= 0)
                return BadRequest("Tutar 0'dan büyük olmalıdır.");

            using (SqlConnection conn = _baglanti.GetConnection())
            {
                conn.Open();
                string query = @"
                    UPDATE GelirGider 
                    SET Turu=@Turu, 
                        KategoriID=@KategoriID, 
                        Aciklama=@Aciklama, 
                        Tutar=@Tutar 
                    WHERE Id=@Id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);                   
                    cmd.Parameters.AddWithValue("@Turu", gelirGider.Turu);
                    cmd.Parameters.AddWithValue("@KategoriID", gelirGider.KategoriID);
                    cmd.Parameters.AddWithValue("@Aciklama", gelirGider.Aciklama ?? "");
                    cmd.Parameters.AddWithValue("@Tutar", gelirGider.Tutar);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                        return NotFound("Kayıt bulunamadı.");
                }
            }

            return Ok("Kayıt güncellendi.");
        }

        // 📌 4️⃣ Kayıt sil
        [HttpDelete("{id}")]
        public IActionResult DeleteGelirGider(int id)
        {
            using (SqlConnection conn = _baglanti.GetConnection())
            {
                conn.Open();
                string query = "DELETE FROM GelirGider WHERE Id=@Id";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);

                    int rows = cmd.ExecuteNonQuery();
                    if (rows == 0)
                        return NotFound("Kayıt bulunamadı.");
                }
            }

            return Ok("Kayıt silindi.");
        }

        // 📌 5️⃣ Toplam gelir ve gider hesapla
        [HttpGet("toplam")]
        public IActionResult GetToplamGelirGider()
        {
            decimal toplamGelir = 0;
            decimal toplamGider = 0;

            using (SqlConnection conn = _baglanti.GetConnection())
            {
                conn.Open();
                string gelirQuery = "SELECT ISNULL(SUM(Tutar), 0) FROM GelirGider WHERE Turu = 'Gelir'";
                string giderQuery = "SELECT ISNULL(SUM(Tutar), 0) FROM GelirGider WHERE Turu = 'Gider'";

                using (SqlCommand gelirCmd = new SqlCommand(gelirQuery, conn))
                using (SqlCommand giderCmd = new SqlCommand(giderQuery, conn))
                {
                    toplamGelir = Convert.ToDecimal(gelirCmd.ExecuteScalar());
                    toplamGider = Convert.ToDecimal(giderCmd.ExecuteScalar());
                }
            }

            return Ok(new 
            { 
                ToplamGelir = toplamGelir, 
                ToplamGider = toplamGider, 
                NetTutar = toplamGelir - toplamGider 
            });
        }

        [HttpGet("kategoriozet")]
        public IActionResult GetKategoriOzet()
        {
            var result = new List<object>();

            using (var conn = _baglanti.GetConnection())
            {
                conn.Open();
                string query = @"
            SELECT k.KategoriAdi AS Kategori, k.Tur, COUNT(*) AS KayitSayisi, 
                   SUM(g.Tutar) AS ToplamTutar
            FROM GelirGider g
            INNER JOIN Kategoriler k ON g.KategoriId = k.KategoriId
            GROUP BY k.KategoriAdi, k.Tur
            ORDER BY CASE WHEN k.Tur='Gelir' THEN 0 ELSE 1 END, k.KategoriAdi
        ";

                using (var cmd = new SqlCommand(query, conn))
                using (var dr = cmd.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        result.Add(new
                        {
                            kategori = dr["Kategori"].ToString(),
                            kayitSayisi = Convert.ToInt32(dr["KayitSayisi"]),
                            toplamTutar = Convert.ToDecimal(dr["ToplamTutar"]),
                            tur = dr["Tur"].ToString()
                        });
                    }
                }
            }

            return Ok(result);
        }




    }
}
