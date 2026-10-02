using System.Collections.Generic;

namespace ShiftManagerApi2
{
    public class ShiftDateItem
    {
        public DateTime Date { get; set; } 
        public string Mode { get; set; } = string.Empty;
    }
    public class EventAnswerItem
    {
        public int Id { get; set; }

        public bool Answer { get; set; } = false;
    }
    
    public class ShiftSubmission
    {
        // 👤 スタッフ名 (userName) を受け取る
        public string Name { get; set; } = string.Empty;

        public string Memo { get; set; } = string.Empty;

        public string id { get; set; } = string.Empty;

        public int Year { get; set; } 

        public int Month { get; set; } 

        // 📅 選択された日付のリスト (sortedDates) を受け取る
        public List<ShiftDateItem> Dates { get; set; }

        public List<EventAnswerItem> Answer { get; set; }
        
    }
}
