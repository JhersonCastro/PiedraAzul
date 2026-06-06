using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PiedraAzul.Domain.Entities.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiedraAzul.Infrastructure.Persistence.Configurations
{
    public class AppointmentBackgroundJobsRecordsConfig : IEntityTypeConfiguration<AppointmentBackgroundJobsRecords>
    {
        public void Configure(EntityTypeBuilder<AppointmentBackgroundJobsRecords> builder)
        {
            builder.ToTable("AppointmentBackgroundJobsRecords");
            
            builder.HasKey(x => x.JobId);

            builder.Property(x => x.AppointmentId);

            // Index on AppointmentId for faster lookups
            builder.HasIndex(x => x.AppointmentId);
        }
    }
}
