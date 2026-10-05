using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic;
using Npgsql;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.ObjectiveC;
using System.Security.Cryptography;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace ShiftManagerApi2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BuildController : ControllerBase
    {
        private readonly DatabaseConnection _db = new DatabaseConnection();
        [HttpGet("calendar")]//いつのカレンダーを表示数かをindex.htmlに返す
        public IActionResult GetCalendar()
        {

            using (var conn = _db.CreateConnection())
            {

                string calendar_sql = "SELECT id,year,month FROM shift_periods WHERE status = '配信中'";
                using (var calendar_cmd = new NpgsqlCommand(calendar_sql, conn))
                {
                    using (var result = calendar_cmd.ExecuteReader())
                    {
                        if (result.Read())
                        {
                            var singleCalendar = new
                            {
                                id = Convert.ToInt32(result["id"]),
                                year = Convert.ToInt32(result["year"]),
                                month = Convert.ToInt32(result["month"])
                            };
                            return Ok(singleCalendar);
                        }
                    }
                }
            }
            return NotFound(); // 見つからなかった場合の処理404を返す
        }

        [HttpGet("bullidcalender")]
        public IActionResult GetCalendar([FromQuery(Name = "Getyear")] int? year, [FromQuery(Name = "Getmonth")] int? month)
        {
            using (var conn = _db.CreateConnection())
            {
                string GetCalendar_sql = "SELECT id,start_date,end_date,status FROM shift_periods WHERE year = @year AND month = @month";
                using (var GetCalendar_cmd = new NpgsqlCommand(GetCalendar_sql, conn))
                {
                    GetCalendar_cmd.Parameters.AddWithValue("@year", year == null ? DBNull.Value : year);
                    GetCalendar_cmd.Parameters.AddWithValue("@month", month == null ? DBNull.Value : month);
                    Console.WriteLine("ここまで０６６");
                    using (var result = GetCalendar_cmd.ExecuteReader())
                    {
                        Console.WriteLine("ここまで０６６５");
                        if (result.Read())
                        {
                            var calendar = new
                            {
                                id = Convert.ToInt32(result["id"]),
                                start_date = ((DateOnly)result["start_date"]).ToString("yyyy-MM-dd"),
                                end_date = ((DateOnly)result["end_date"]).ToString("yyyy-MM-dd"),
                                status = result["status"] == DBNull.Value ? "未配信" : result["status"].ToString()

                            };
                            Console.WriteLine("ここまで０６６６");
                            return Ok(calendar);
                        }
                        else
                        {
                            var calendar = new
                            {
                                id = (int?)null,
                                start_date = "未作成",
                                end_date = "未作成",
                                status = "未作成",
                            };
                            Console.WriteLine("ここまで０６６７");
                            return Ok(calendar);
                        }

                    }

                }
            }
            return NotFound(); // 見つからなかった場合の処理404を返す
        }
        [HttpGet("event")]
        public IActionResult GetEvent([FromQuery(Name = "GetId")] int? id)
        {
            Console.WriteLine("ここまで０");
            var eventList = new List<object>();
            using (var conn = _db.CreateConnection())
            {
                string GetEvent_sql = "SELECT id,name,day FROM event WHERE periods_id = @id";
                Console.WriteLine("ここまで１");
                using (var GetEvent_cmd = new NpgsqlCommand(GetEvent_sql, conn))
                {
                    Console.WriteLine("ここまで２");
                    GetEvent_cmd.Parameters.AddWithValue("@id", id == null ? DBNull.Value : id);
                    using (var result = GetEvent_cmd.ExecuteReader())
                    {
                        Console.WriteLine("ここまで３");

                        while (result.Read())
                        {
                            var events = new
                            {
                                name = result["name"].ToString(),
                                day = Convert.ToInt32(result["day"]),
                                id = Convert.ToInt32(result["id"])
                            };
                            Console.WriteLine(events);
                            eventList.Add(events);
                        }
                        return Ok(eventList);
                    }
                }
            }
        }
        [HttpGet("CreatePeriods")]
        public IActionResult CreatePeriods([FromQuery(Name = "GetYear")] int Year, [FromQuery(Name = "GetMonth")] int Month)
        {
            // 🎯 1. 送られてきた year と month から「開始日（1日）」と「終了日（末日）」を自動計算する
            DateOnly StartDate = new DateOnly(Year, Month, 1);
            int lastDay = DateTime.DaysInMonth(Year, Month); // その月の最終日（30, 31, 28など）を取得
            DateOnly EndDate = new DateOnly(Year, Month, lastDay);
            using (var conn = _db.CreateConnection())
            {
                string CreatePeriods_sql = "INSERT INTO shift_periods (year,month,start_date,end_date,status) VALUES (@year,@month,@startDate,@endDate,@status)";
                using (var CreatePeriods_cmd = new NpgsqlCommand(CreatePeriods_sql, conn))
                {
                    CreatePeriods_cmd.Parameters.AddWithValue("@year", Year);
                    CreatePeriods_cmd.Parameters.AddWithValue("@month", Month);
                    CreatePeriods_cmd.Parameters.AddWithValue("@startDate", StartDate);
                    CreatePeriods_cmd.Parameters.AddWithValue("@endDate", EndDate);
                    CreatePeriods_cmd.Parameters.AddWithValue("@status", "未配信");
                    int rowsAffected = CreatePeriods_cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return Ok(new { message = "新しいシフト期間が作成されました。" });
                    }
                    else
                    {
                        return BadRequest(new { message = "シフト期間の作成に失敗しました。" });
                    }
                }
            }
        }
        [HttpGet("UpdateStatus")]//配信中のやつを未配信のやつを配信中に変える
        public IActionResult UpdateStatus([FromQuery(Name = "GetId")] int Id)
        {
            using (var conn = _db.CreateConnection())
            {
                string reset_sql = "UPDATE shift_periods SET status = '未配信'";
                using (var reset_cmd = new NpgsqlCommand(reset_sql, conn))
                {
                    reset_cmd.ExecuteNonQuery();
                }
                
                string updateSql = "UPDATE shift_periods SET status = '配信中' WHERE id = @id RETURNING year,month";
                using (var updateCmd = new NpgsqlCommand(updateSql, conn))
                {
                    updateCmd.Parameters.AddWithValue("@id", Id);
                    using (var reader = updateCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            int year = reader.GetInt32(0);
                            int month = reader.GetInt32(1);

                            return Ok(new
                            {
                                year = year,
                                month = month
                            });
                        }
                        else
                        {
                            return BadRequest(new { message = "指定されたIDが見つかりませんでした。" });
                        }
                    }

                    
                }
            }
        }
        [HttpGet("modalbodyBtn")]
        public IActionResult InsertEvent([FromQuery(Name ="GetId") ] int Id,[FromQuery(Name = "GetDay") ]int day, [FromQuery(Name = "GetText")] string Text, [FromQuery(Name = "GetContent")] string Content)
        {
            Console.WriteLine("ここまでイベント222");
            using (var conn = _db.CreateConnection())
            {
                Console.WriteLine("ここまでイベント20");
                string InsertEvent_sql = "INSERT INTO event(periods_id,day,name,content) VALUES(@periods_id,@day,@name,@content)";
                using(var InsertEvent_cmd = new NpgsqlCommand(InsertEvent_sql, conn))
                {
                    InsertEvent_cmd.Parameters.AddWithValue("@periods_id",Id);
                    InsertEvent_cmd.Parameters.AddWithValue("@day", day);
                    InsertEvent_cmd.Parameters.AddWithValue("@name", Text);
                    InsertEvent_cmd.Parameters.AddWithValue("@content", Content);
                    Console.WriteLine("ここまでイベント1");
                    int rowsAffected = InsertEvent_cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        Console.WriteLine("ここまでイベント2");
                        return Ok(new { message = "新しいイベントが作成されました。" });
                    }
                    else
                    {
                        return BadRequest(new { message = "イベントの作成に失敗しました。" });
                    }
                }
            }
        }

        [HttpGet("Deleteevent")]
        public IActionResult DeleteEvent([FromQuery(Name = "GetId")] int Id)
        {
            using(var conn = _db.CreateConnection())
            {
                string DeleteEvent_sql = "DELETE FROM event WHERE id =@id";
                using(var DeleteEvent_cmd = new NpgsqlCommand(DeleteEvent_sql, conn))
                {
                    DeleteEvent_cmd.Parameters.AddWithValue("@id", Id);
                    int rowsAffected =DeleteEvent_cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return Ok(new { message = "新しいシフト期間が作成されました。" });
                    }
                    else
                    {
                        return BadRequest(new { message = "シフト期間の作成に失敗しました。" });
                    }
                }
            }
        }

    }
}
