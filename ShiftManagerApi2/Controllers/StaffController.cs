using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualBasic;
using Npgsql;
using System.Collections.Generic;
using System.Runtime.InteropServices.ObjectiveC;
using System.Security.Cryptography;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace ShiftManagerApi2.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class StaffController : ControllerBase
    {
        private readonly DatabaseConnection _db = new DatabaseConnection();

        [HttpGet("stafflist")]
        public IActionResult GetStaffList()
        {
            using (var conn = _db.CreateConnection())
            {
                string sql = @"SELECT staff.id, staff.staff_name,  staff.role,  j.job_name FROM staff INNER JOIN job e     ON e.staff_id = staff.id INNER JOIN job_master j ON j.job_id = e.job_id ORDER BY staff.id ASC;";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    using (var result = cmd.ExecuteReader())
                    {
                        var staffList = new List<object>();
                        while (result.Read())
                        {
                            var staff = new
                            {
                                id = Convert.ToInt32(result["id"]),
                                staff_name = result["staff_name"].ToString(),
                                role = result["role"].ToString(),
                                job_name = result["job_name"].ToString()
                            };
                            staffList.Add(staff);
                        }
                        return Ok(staffList);
                    }
                }
            }
        }
        [HttpGet("joblist")]
        public IActionResult GetJobList()
        {
            using (var conn = _db.CreateConnection())
            {
                string sql = @"SELECt job_name FROM job_master";
                using (var cmd = new NpgsqlCommand(sql, conn))
                {
                    using (var result = cmd.ExecuteReader())
                    {
                        var jobList = new List<string>();
                        while (result.Read())
                        {
                            jobList.Add(result["job_name"].ToString());
                        }
                        return Ok(jobList);
                    }
                }
            }

        }
        [HttpPost("injob")]
        public IActionResult InnerJob([FromQuery(Name = "staffId")] int staff_Id, [FromQuery(Name = "jobname")] string job_name)
        {

            using (var conn = _db.CreateConnection())
            {
                string get_id_sql = @"SELECT job_id FROM job_master WHERE job_name = @job_name";
                int job_Id = 0;
                using (var get_id_cmd = new NpgsqlCommand(get_id_sql, conn))
                {
                    get_id_cmd.Parameters.AddWithValue("@job_name", job_name);
                    var result = get_id_cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        job_Id = Convert.ToInt32(result);
                    }
                    else
                    {
                        return BadRequest("指定された職種が存在しません。");
                    }

                }
                string inner_job_sql = @"INSERT INTO job(staff_id,job_id) VALUES(@staff_id,@job_id)";
                using (var inner_job_cmd = new NpgsqlCommand(inner_job_sql, conn))
                {
                    inner_job_cmd.Parameters.AddWithValue("@staff_id", staff_Id);
                    inner_job_cmd.Parameters.AddWithValue("@job_id", job_Id);
                    int rowsAffected = inner_job_cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return Ok(new { message = "職種の追加に成功しました。" });
                    }
                    else
                    {
                        return BadRequest(new { message = "職種の追加に失敗しました。" });
                    }
                }
            }
        }
        [HttpGet("updaterole")]

        public IActionResult changerole([FromQuery(Name = "staffId")] int staff_Id, [FromQuery(Name = "rolename")] string rol_name)
        {

            using (var conn = _db.CreateConnection())
            {
                string upcate_role_sql = @"UPDATE staff SET role = @rol_name WHERE id = @staff_id";
                int job_Id = 0;
                using (var upcate_role_cmd = new NpgsqlCommand(upcate_role_sql, conn))
                {
                    upcate_role_cmd.Parameters.AddWithValue("@rol_name", rol_name);
                    upcate_role_cmd.Parameters.AddWithValue("@staff_id", staff_Id);
                    var result = upcate_role_cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return Ok(new { message = "ロールがないよ" });
                    }
                    else
                    {

                        return Ok(new { message = "ロールの変更が完了しました" });
                    }

                }

            }

        }
        [HttpGet("deljob")]

        public IActionResult deljob([FromQuery(Name = "staffId")] int staff_Id, [FromQuery(Name = "jobname")] string job_name)
        {

            using (var conn = _db.CreateConnection())
            {
                string get_id_sql = @"SELECT job_id FROM job_master WHERE job_name = @job_name";
                int job_Id = 0;
                using (var get_id_cmd = new NpgsqlCommand(get_id_sql, conn))
                {
                    get_id_cmd.Parameters.AddWithValue("@job_name", job_name);
                    var result = get_id_cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        job_Id = Convert.ToInt32(result);
                    }
                    else
                    {
                        return Ok(new { message = "職種がないよ" });
                    }

                }
                string check_count_sql = @"SELECT COUNT(*) FROM job WHERE staff_id = @staff_id";
                using (var check_count_cmd = new NpgsqlCommand(check_count_sql, conn))
                {
                    check_count_cmd.Parameters.AddWithValue("@staff_id", staff_Id);

                    // COUNT(*) の結果を取得（PostgreSQLのCOUNTはlong型で返る）
                    long currentCount = (long)check_count_cmd.ExecuteScalar();

                    // 1件以下しかない場合は削除させない（最低1つは残す）
                    if (currentCount <= 1)
                    {
                        return Ok(new { message = "１つ以上の職種がいるよ" });
                    }
                }
                string inner_job_sql = @"DELETE FROM job WHERE staff_id = @staff_id AND job_id = @job_Id";
                using (var inner_job_cmd = new NpgsqlCommand(inner_job_sql, conn))
                {
                    inner_job_cmd.Parameters.AddWithValue("@staff_id", staff_Id);
                    inner_job_cmd.Parameters.AddWithValue("@job_id", job_Id);
                    int rowsAffected = inner_job_cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return Ok(new { message = "削除に成功したよ" });
                    }
                    else
                    {
                        return Ok(new { message = "削除に失敗したよ" });
                    }
                }
            }

        }
        [HttpGet("injobmaster")]

        public IActionResult injobmaster([FromQuery(Name = "jobname")] string jobname)
        {
            Console.WriteLine(jobname);
            using (var conn = _db.CreateConnection())
            {
                string job_id_sql = @"SELECT MAX(job_id)+1 FROM job_master";//新しいjob_idの作成のため一番デカいjob_idを取得するし＋1をする
                int jobmax_Id = 0;
                using (var job_role_cmd = new NpgsqlCommand(job_id_sql, conn))
                {

                    var result = job_role_cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        jobmax_Id = Convert.ToInt32(result);
                        
                    }
                    else
                    {
                        Console.WriteLine(jobname + 3);
                        //return Ok(new { message = "最大値が取得できなかった" });
                    }

                }


                string upcate_role_sql = @"INSERT INTO job_master (job_id,job_name) VALUES(@job_id,@job_name)";

                using (var upcate_role_cmd = new NpgsqlCommand(upcate_role_sql, conn))
                {
                    upcate_role_cmd.Parameters.AddWithValue("@job_id", jobmax_Id);
                    upcate_role_cmd.Parameters.AddWithValue("@job_name", jobname);
                    var result = upcate_role_cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        Console.WriteLine(jobname + 1);
                        return Ok(new { message = "ロールがないよ" });

                    }
                    else
                    {
                        Console.WriteLine(jobname + 2);
                        return Ok(new { message = "追加に成功しました" });
                    }

                }

            }

        }
        [HttpGet("inManualstaff")]

        public IActionResult InManualstaff([FromQuery(Name = "name")] string name, [FromQuery(Name = "line_id")] string line_id, [FromQuery(Name = "role")] string role, [FromQuery(Name = "job")] string job)
        {

            using (var conn = _db.CreateConnection())
            {
                string get_id_sql = @"SELECT job_id FROM job_master WHERE job_name = @job";
                int job_Id = 0;
                int staff_id = 0;
                using (var get_id_cmd = new NpgsqlCommand(get_id_sql, conn))
                {
                    get_id_cmd.Parameters.AddWithValue("@job", job);
                    var result = get_id_cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        job_Id = Convert.ToInt32(result);
                    }
                    else
                    {
                        return Ok(new { message = "職種がないよ" });
                    }

                }
                string insert_staff_sql = @"INSERT INTO  staff(staff_name,line_id,job,role) VALUES(@name,@line_id,0,@role)RETURNING id";

                using (var insert_staff_cmd = new NpgsqlCommand(insert_staff_sql, conn))
                {
                    insert_staff_cmd.Parameters.AddWithValue("@role", role);
                    insert_staff_cmd.Parameters.AddWithValue("@name", name);
                    insert_staff_cmd.Parameters.AddWithValue("@line_id", line_id);
                    var result = insert_staff_cmd.ExecuteScalar();

                    if (result != null && result != DBNull.Value)
                    {
                        staff_id = Convert.ToInt32(result);
                    }
                    else
                    {
                        return Ok(new { message = "ロールがないよ" });
                    }

                }
                string inner_job_sql = @"INSERT INTO job(staff_id,job_id) VALUES(@staff_id,@job_id)";
                using (var inner_job_cmd = new NpgsqlCommand(inner_job_sql, conn))
                {
                    inner_job_cmd.Parameters.AddWithValue("@staff_id", staff_id);
                    inner_job_cmd.Parameters.AddWithValue("@job_id", job_Id);
                    int rowsAffected = inner_job_cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {

                    }
                    else
                    {
                        return BadRequest(new { message = "ロールの追加に失敗しました。" });
                    }
                }
                string update_staff_sql = @"update staff set job = @staff_id WHERE id = @stafff_id";
                using (var update_staff_cmd = new NpgsqlCommand(update_staff_sql, conn))
                {
                    update_staff_cmd.Parameters.AddWithValue("@staff_id", staff_id);
                    update_staff_cmd.Parameters.AddWithValue("@stafff_id", staff_id);
                    int rowsAffected = update_staff_cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {

                    }
                    else
                    {
                        return BadRequest(new { message = "ロールの追加に失敗しました。" });
                    }
                }

                return Ok(new { message = "登録が完了しました", id = staff_id });

            }

        }

        [HttpGet("staffapplicationlist")]
        public IActionResult staffapplicationlist()
        {

            using (var conn = _db.CreateConnection())
            {
                string staff_appl_sql = @"SELECT * from staff_apply WHERE status=0 ";//一旦承認済みじゃないやつだけ表示必要に応じて未承諾承認済みを表示するようにする
                using (var staff_appl_cmd = new NpgsqlCommand(staff_appl_sql, conn))
                {
                    using (var result = staff_appl_cmd.ExecuteReader())
                    {
                        var staffList = new List<object>();
                        while (result.Read())
                        {
                            var staff = new
                            {
                                id = Convert.ToInt32(result["id"]),
                                line_id = result["line_id"].ToString(),
                                name = result["name"].ToString(),
                                status = Convert.ToInt32(result["status"])
                            };
                            staffList.Add(staff);
                        }
                        return Ok(staffList);
                    }

                }

            }
        }
        [HttpGet("staffapplicationsCheck")]
        public IActionResult staffapplicationsCheckt([FromQuery(Name = "id")] int id, [FromQuery(Name = "count")] int count)
        {

            using (var conn = _db.CreateConnection())
            {
                string staff_appl_sql = @"UPDATE staff_apply SET status = @count WHERE id = @id ";//一旦承認済みじゃないやつだけ表示必要に応じて未承諾承認済みを表示するようにする
                using (var staff_appl_cmd = new NpgsqlCommand(staff_appl_sql, conn))
                {
                    staff_appl_cmd.Parameters.AddWithValue("@id", id);
                    staff_appl_cmd.Parameters.AddWithValue("@count", count);
                    int rowsAffected = staff_appl_cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {

                    }
                    else
                    {
                        return BadRequest(new { message = "登録に失敗しました。" });
                    }
                }
                return Ok(new { message = "登録が完了しました" });

            }

        }
        [HttpGet("deljobmaster")]
        public IActionResult deljobmaster([FromQuery(Name = "jobname")] string job_name)
        {

            using (var conn = _db.CreateConnection())
            {
                string get_id_sql = @"SELECT job_id FROM job_master WHERE job_name = @job_name";
                int job_Id = 0;
                using (var get_id_cmd = new NpgsqlCommand(get_id_sql, conn))
                {
                    get_id_cmd.Parameters.AddWithValue("@job_name", job_name);
                    var result = get_id_cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        job_Id = Convert.ToInt32(result);
                    }
                    else
                    {
                        return Ok(new { message = "職種がないよ" });
                    }

                }
                string check_id_sql = @"SELECT job_id FROM job WHERE job_id =@job_Id";
                using (var check_id_cmd = new NpgsqlCommand(check_id_sql, conn))
                {
                    check_id_cmd.Parameters.AddWithValue("@job_Id", job_Id);
                    var result = check_id_cmd.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        return Ok(new { message = "全てのスタッフから削除される職種をなくしてください" });

                    }

                }
                string delete_id_sql = @"DELETE FROM job_master WHERE job_id = @job_Id";
                using (var delete_id_cmd = new NpgsqlCommand(delete_id_sql, conn))
                {
                    delete_id_cmd.Parameters.AddWithValue("@job_Id", job_Id);
                    var result = delete_id_cmd.ExecuteScalar();

                    return Ok(new { message = "削除成功" });
                }

            }
        }

        [HttpGet("namechangename")]
        public IActionResult namechangename([FromQuery(Name = "staffId")] int id, [FromQuery(Name = "newName")] string newName)
        {

            using (var conn = _db.CreateConnection())
            {
                string staff_appl_sql = @"UPDATE staff SET staff_name = @newName WHERE id = @id ";//一旦承認済みじゃないやつだけ表示必要に応じて未承諾承認済みを表示するようにする
                using (var staff_appl_cmd = new NpgsqlCommand(staff_appl_sql, conn))
                {
                    staff_appl_cmd.Parameters.AddWithValue("@id", id);
                    staff_appl_cmd.Parameters.AddWithValue("@newName", newName);
                    int rowsAffected = staff_appl_cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return Ok(new { message = "名前を変更しました。" });
                    }
                    else
                    {
                        return BadRequest(new { message = "変更に失敗しました。（対象のスタッフが存在しません）" });
                    }


                }

            }
        }
        [HttpPost("deletestaff")]
        public IActionResult deletestaff([FromQuery(Name = "staffId")] int id)
        {

            using (var conn = _db.CreateConnection())
            {
                string delete_appl_sql = @"DELETE FROM staff WHERE id = @id ";//一旦承認済みじゃないやつだけ表示必要に応じて未承諾承認済みを表示するようにする
                using (var delete_appl_cmd = new NpgsqlCommand(delete_appl_sql, conn))
                {
                    delete_appl_cmd.Parameters.AddWithValue("@id", id);
                    int rowsAffected = delete_appl_cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        return Ok(new { message = "削除に成功しました" });
                    }
                    else
                    {
                        return BadRequest(new { message = "削除に失敗しました" });
                    }


                }

            }
        }
    }
}



