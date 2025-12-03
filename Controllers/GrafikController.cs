using Microsoft.AspNetCore.Mvc;
using MuhasebeAPI.Helpers;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;

namespace MuhasebeAPI.Controllers
{
    public class AyVerisi
    {
        public decimal Gelir { get; set; }
        public decimal Gider { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class GrafikController : ControllerBase
    {
        private readonly Baglanti _baglanti = new Baglanti();

        [HttpGet("yillar")]
        public IActionResult GetYillar()
        {
            var yillar = new List<int>();
            try
            {
                using (SqlConnection conn = _baglanti.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("SELECT DISTINCT YEAR(Tarih) AS Yil FROM GelirGider ORDER BY Yil DESC", conn))
                    using (SqlDataReader dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                            yillar.Add(Convert.ToInt32(dr["Yil"]));
                    }
                }

                if (!yillar.Any())
                    return NotFound(new { message = "Veritabanında hiç kayıt bulunamadı." });

                return Ok(yillar);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Yıllar alınamadı: " + ex.Message });
            }
        }

        [HttpGet("{yil}")]
        public IActionResult GetYillikGelirGider(int yil)
        {
            try
            {
                // AyNo -> AyVerisi
                var dict = new Dictionary<int, AyVerisi>();

                string query = @"
                    SELECT 
                        MONTH(Tarih) AS AyNo,
                        SUM(CASE WHEN Turu = 'Gelir' THEN Tutar ELSE 0 END) AS Gelir,
                        SUM(CASE WHEN Turu = 'Gider' THEN Tutar ELSE 0 END) AS Gider
                    FROM GelirGider
                    WHERE YEAR(Tarih) = @yil
                    GROUP BY MONTH(Tarih)
                    ORDER BY MONTH(Tarih);
                ";

                using (SqlConnection conn = _baglanti.GetConnection())
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@yil", yil);
                        using (SqlDataReader dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                int ayNo = dr["AyNo"] == DBNull.Value ? 0 : Convert.ToInt32(dr["AyNo"]);
                                decimal gelir = dr["Gelir"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Gelir"]);
                                decimal gider = dr["Gider"] == DBNull.Value ? 0m : Convert.ToDecimal(dr["Gider"]);

                                if (ayNo > 0 && !dict.ContainsKey(ayNo))
                                    dict[ayNo] = new AyVerisi { Gelir = gelir, Gider = gider };
                            }
                        }
                    }
                }

                if (dict.Count == 0)
                    return NotFound(new { message = $"{yil} yılı için veri bulunamadı." });

                var tr = new CultureInfo("tr-TR");
                var result = new List<object>();

                for (int ay = 1; ay <= 12; ay++)
                {
                    var val = dict.ContainsKey(ay) ? dict[ay] : new AyVerisi();
                    string ayAdi = new DateTime(2000, ay, 1).ToString("MMM", tr);

                    result.Add(new
                    {
                        AyNo = ay,
                        Ay = ayAdi,
                        Gelir = val.Gelir,
                        Gider = val.Gider
                    });
                }

                return Ok(result);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(500, new { message = "Veritabanı hatası: " + sqlEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Grafik verileri alınamadı: " + ex.Message });
            }
        }
    }
}
