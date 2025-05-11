namespace МessageAir.Models
{
    public class MessageGroup: List<MessageModel>
    {
        public string Date { get; }
        public DateTime SortableDate { get; }

        public MessageGroup(string date, DateTime sortableDate, List<MessageModel> messages) : base(messages)
        {
            Date = date;
            SortableDate = sortableDate;
        }
    }
}
