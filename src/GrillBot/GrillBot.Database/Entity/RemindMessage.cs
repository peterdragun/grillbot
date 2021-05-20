﻿using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GrillBot.Database.Entity
{
    public class RemindMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        [StringLength(30)]
        public string FromUserId { get; set; }

        [ForeignKey(nameof(FromUserId))]
        public User FromUser { get; set; }

        [Required]
        [StringLength(30)]
        public string ToUserId { get; set; }

        [ForeignKey(nameof(ToUserId))]
        public User ToUser { get; set; }

        [Required]
        public DateTime At { get; set; }

        [Required]
        public string Message { get; set; }

        [Required]
        public int Postpone { get; set; } = 0;

        [StringLength(30)]
        public string OriginalMessageId { get; set; }
    }
}