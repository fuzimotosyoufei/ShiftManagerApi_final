using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Npgsql;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices.ObjectiveC;
using System.Security.Cryptography;
using System.Xml.Linq;
//using static System.Runtime.InteropServices.JavaScript.JSType;
namespace ShiftManagerApi2.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase

    {
        private readonly DatabaseConnection _db = new DatabaseConnection();
        [HttpGet("calendarnau")]
        public IActionResult GetCalendarNau()//今配信しているカレンダーと従業員の名前をとイベントの取得
        {
            //Console.WriteLine(periodis_id);
            var calendarNauList = new List<object>();
            using (var conn = _db.CreateConnection())
            {
                string calendarNau_sql = "SELECT s.id AS staff_id, s.staff_name AS name , s.line_id as line_id ,rs.id AS req_id,rs.memo AS memo  FROM staff s LEFT OUTER JOIN(SELECT rs.id, rs.staff_id,rs.memo FROM shift_reqs rs INNER JOIN(SELECT p.id FROM shift_periods p WHERE p.status = '配信中') p ON rs.periods_id = p.id)rs ON rs.staff_id = s.id ORDER BY s.id ASC";
                using (var calendarNau_cmd = new NpgsqlCommand(calendarNau_sql, conn))
                {
                    using (var result = calendarNau_cmd.ExecuteReader())
                    {
                        while (result.Read())
                        {
                            var reqIdValue = result["req_id"];//Nullの可能性があるからここではint型にしない
                            List<object> ShiftDay = new List<object>();
                            List<object> ShiftEvent = new List<object>();
                            if (reqIdValue != DBNull.Value)
                            {
                                int reqId = Convert.ToInt32(reqIdValue);
                                ShiftDay = GetShiftReqDates(reqId);
                                ShiftEvent = Calender_GetEvent(reqId);
                                //Console.WriteLine("YEsの実行");
                            }
                            else
                            {
                                ShiftDay = null;
                                //Console.WriteLine("NUllのじっこう");
                            }
                            var SingreDate = new
                            {
                                id = Convert.ToString(result["staff_id"]),
                                name = result["name"].ToString(),
                                line_id = result["line_id"].ToString(),
                                Event = ShiftEvent,
                                day = ShiftDay,
                                memo = Convert.ToString(result["memo"])
                            };
                            calendarNauList.Add(SingreDate);
                            //calendarNauList.Add(shift_req_dates(Convert.ToInt32(result["id"])));
                        }
                    }
                }
            }
            return Ok(calendarNauList);
        }

        private List<object> Calender_GetEvent(int req_id)
        {
            var EventList = new List<object>();
            using (var conn = _db.CreateConnection())
            {
                string event_sql = "SELECT e.name,a.answer FROM shift_reqs rs CROSS JOIN(SELECT e.name, e.periods_id, e.id FROM event e INNER JOIN (SELECT p.id FROM shift_periods p WHERE p.status = '配信中')p on p.id = e.periods_id)e LEFT outer JOIN event_answer a ON  a.reqs_id =rs.id AND a.event_id = e.id WHERE rs.id = @req_id ORDER BY rs.id asc, e.id asc";
                using (var event_cmd = new NpgsqlCommand(event_sql, conn))
                {

                    event_cmd.Parameters.AddWithValue("@req_id", req_id);
                    using (var result = event_cmd.ExecuteReader())
                    {
                        while (result.Read())
                        {
                            //Console.WriteLine(result["answer"]);
                            //var SingleEvent = new
                            //{
                            //    name = result["name"].ToString(),
                            //};
                            //EventList.Add(SingleEvent);
                            var SingreEvent = new Dictionary<string, object>
                            {

                                { result["name"].ToString(),result["answer"] == DBNull.Value ? (bool?)null : Convert.ToBoolean(result["answer"])},
                                { "name",result["name"].ToString() }

                            };

                            EventList.Add(SingreEvent);
                        }
                        return EventList;
                    }
                }
            }
        }

        private List<object> GetShiftReqDates(int req_id)
        {
            //Console.WriteLine("GetShiftReqDates");
            var DataList = new List<object>();
            using (var conn = _db.CreateConnection())
            {

                string shift_req_dates_sql = "SELECT date,mode FROM shift_req_dates WHERE req_id = @req_id";
                using (var updates_cmd = new NpgsqlCommand(shift_req_dates_sql, conn))
                {
                    updates_cmd.Parameters.AddWithValue("@req_id", req_id);
                    using (var result = updates_cmd.ExecuteReader())
                    {
                        while (result.Read())
                        {
                            var SingreDate = new
                            {
                                date = ((DateOnly)result["date"]).ToString("yyyy-MM-dd"),
                                mode = result["mode"].ToString()
                            };
                            DataList.Add(SingreDate);
                        }
                    }
                }
            }
            return DataList;
        }

        [HttpGet("YearMonth")]
        public IActionResult YearAndMonth()
        {
            Console.WriteLine("Year実行「");
            using (var conn = _db.CreateConnection())
            {
                string ym_sql = "SELECT id,year,month FROM shift_periods WHERE status = '配信中'";
                using (var ym_cmd = new NpgsqlCommand(ym_sql, conn))
                {
                    using (var result = ym_cmd.ExecuteReader())
                    {
                        Console.WriteLine("s処理Year実行「");
                        if (result.Read())
                        {
                            Console.WriteLine("se処理Year実行「");
                            var SingreYM = new
                            {
                                id = Convert.ToInt32(result["id"]),
                                year = Convert.ToInt32(result["year"]),
                                month = Convert.ToInt32(result["month"])
                            };
                            Console.WriteLine("seee処理Year実行「");
                            return Ok(SingreYM);
                        }

                    }
                }
            }
            Console.WriteLine("sssss処理Year実行「");
            return NotFound("配信中のシフト期間が見つかりませんでした。");//IActionResultを使うなら絶対に失敗したときの条件がいる
        }


        [HttpPost("staffinshift")]
        public IActionResult GetReqID([FromQuery(Name = "GetId")] int staff_id, [FromQuery(Name = "dateStr")] DateTime date, [FromQuery(Name = "selectedValue")] string? selectedValue, [FromQuery(Name = "year")] int year, [FromQuery(Name = "month")] int month)//その月にすでに登録されているかどうかを確認する処理、なければ新規登録する処理に行く、あればそのIDを返す処理に行く
        {
            using (var conn = _db.CreateConnection())
            {
                string reqs_sql = "  SELECT shift_reqs.id FROM shift_reqs WHERE staff_id = @staff_id AND shift_reqs.periods_id = (SELECT shift_periods.id FROM shift_periods WHERE shift_periods.year = @year AND shift_periods.month = @month) ";

                using (var reqs_cmd = new NpgsqlCommand(reqs_sql, conn))
                {
                    reqs_cmd.Parameters.AddWithValue("@staff_id", staff_id);
                    reqs_cmd.Parameters.AddWithValue("@year", year);
                    reqs_cmd.Parameters.AddWithValue("@month", month);

                    var result = reqs_cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value)//ない処理
                    {
                        Console.WriteLine("s処理Yea新しく作成「");
                        Insertreqs(staff_id, date, selectedValue, year, month);
                        return Ok(new { message = "追加がが完了しました" });
                    }
                    else//ある処理
                    {
                        Console.WriteLine("s処理No追加か更新「");
                        int req_id = Convert.ToInt32(result);
                        Checkshift_req(req_id, staff_id, date, selectedValue, year, month);
                        return Ok(new { message = "更新" });
                    }

                }
            }
        }
        private void Insertreqs(int staff_id, DateTime date, string? selectedValue, int year, int month)//日付がいる
        {
            using (var conn = _db.CreateConnection())
            {
                string insert_sql = "INSERT INTO shift_reqs (staff_id,periods_id,memo) SELECT @staff_id, id, null FROM shift_periods WHERE year =@year AND month = @month RETURNING id";
                using (var insert_cmd = new NpgsqlCommand(insert_sql, conn))
                {
                    insert_cmd.Parameters.AddWithValue("@staff_id", staff_id);
                    insert_cmd.Parameters.AddWithValue("@year", year);
                    insert_cmd.Parameters.AddWithValue("@month", month);

                    var scalarResult = insert_cmd.ExecuteScalar();
                    int req_id = Convert.ToInt32(scalarResult);
                    Insertreq_dates(req_id, staff_id, date, selectedValue, year, month);



                }
            }
        }
        private void Checkshift_req(int req_id, int staff_id, DateTime date, string? selectedValue, int year, int month)
        {
            using (var conn = _db.CreateConnection())
            {
                string check_sql = "SELECT * FROM shift_req_dates WHERE req_id = @req_id AND date = @date";
                using (var check_cmd = new NpgsqlCommand(check_sql, conn))
                {
                    check_cmd.Parameters.AddWithValue("@req_id", req_id);
                    check_cmd.Parameters.AddWithValue("@date", date);

                    var scalarResult = check_cmd.ExecuteScalar();
                    if (scalarResult == null || scalarResult == DBNull.Value)
                    {
                        Insertreq_dates(req_id, staff_id, date, selectedValue, year, month);//既にない場合の処理
                    }
                    else
                    {
                        Updatereq_dates(req_id, staff_id, date, selectedValue, year, month);
                    }




                }
            }
        }
        private void Insertreq_dates(int req_id, int staff_id, DateTime date, string? selectedValue, int year, int month)//日付がいる
        {
            using (var conn = _db.CreateConnection())
            {
                
              
                    var dates_insert_sql = "INSERT INTO shift_req_dates (req_id, date, mode) VALUES (@req_id, @date, @mode)";
                    using (var insert_dates__cmd = new NpgsqlCommand(dates_insert_sql, conn))
                    {

                        //insert_dates__cmd.Parameters.Clear();
                        insert_dates__cmd.Parameters.AddWithValue("@req_id", req_id);
                        insert_dates__cmd.Parameters.AddWithValue("@date", date);
                        insert_dates__cmd.Parameters.AddWithValue("@mode", selectedValue);
                        insert_dates__cmd.ExecuteNonQuery();

                        //return Convert.ToInt32(req_id);
                        //return Convert.ToInt32(req_id);
                        //}新規処理のエラーが治るまで封印
                    }//新しいshift_req_data追加する処理
                

            }
        }
        private void Updatereq_dates(int req_id, int staff_id, DateTime date, string? selectedValue, int year, int month)//日付がいる
        {
            using (var conn = _db.CreateConnection())
            {
                if (string.IsNullOrEmpty(selectedValue))
                {
                    var dates_insert_sql = "DELETE FROM shift_req_dates WHERE req_id = @req_id AND date = @date";
                    using (var insert_dates__cmd = new NpgsqlCommand(dates_insert_sql, conn))
                    { 
                        insert_dates__cmd.Parameters.AddWithValue("@req_id", req_id);
                        insert_dates__cmd.Parameters.AddWithValue("@date", date);
                        insert_dates__cmd.ExecuteNonQuery();
                    }//新しいshift_req_data追加する処理
                }
                else
                {
                    var dates_update_sql = "UPDATE shift_req_dates SET mode = @mode WHERE req_id = @req_id AND date = @date";
                    using (var update_dates_cmd = new NpgsqlCommand(dates_update_sql, conn))
                    {
                        update_dates_cmd.Parameters.AddWithValue("@req_id", req_id);
                        update_dates_cmd.Parameters.AddWithValue("@date", date);
                        update_dates_cmd.Parameters.AddWithValue("@mode", selectedValue);
                        update_dates_cmd.ExecuteNonQuery();
                    }
                }
                   
            }

        }
    }
}
