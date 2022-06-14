namespace PC2.Models
{
    public class Staff : People
    {
        public string? Phone { get; set; }

        public int? Extension { get; set; }

        public string Email { get; set; }

        public string? PhoneDisplay
        {
            get
            {
                if (Phone != null && Extension != null)
                {
                    return $"{Phone} ext. {Extension}";
                }
                else if (Phone != null)
                {
                    return Phone;
                }
                else
                {
                    return null;
                }
            }
        }
    }
}
