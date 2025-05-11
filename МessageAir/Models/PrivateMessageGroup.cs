namespace МessageAir.Models
{
    public class PrivateMessageGroup: List<PrivateMessageModel>
    {
        public string Date { get; }
        public DateTime SortableDate { get; }

        public PrivateMessageGroup(string date, DateTime sortableDate, List<PrivateMessageModel> messages) : base(messages)
        {
            Date = date;
            SortableDate = sortableDate;
        }
    }
}
