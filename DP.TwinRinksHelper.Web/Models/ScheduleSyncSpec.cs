using System;
using System.ComponentModel.DataAnnotations;

namespace DP.TwinRinksHelper.Web.Models
{
    public class ScheduleSyncSpec
    {
        public int ID { get; set; }
        [Required]
        public int TeamSnapUserId { get; set; }
        public string Recipients { get; set; }
        [Required]
        public string TeamSnapToken { get; set; }
        [Required]
        public long TeamSnapTeamId { get; set; }
        [Required]
        public string TeamSnapTeamName { get; set; }
        [Required]
        public string TwinRinkTeamName { get; set; }
        public DateTime LastChecked { get; set; }
        public DateTime Expires { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime LastUpdated { get; set; }

    }
}
