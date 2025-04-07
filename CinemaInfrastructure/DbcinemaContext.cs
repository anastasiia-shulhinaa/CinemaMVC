using CinemaDomain.Model;
using Microsoft.EntityFrameworkCore;

namespace CinemaInfrastructure;

public partial class DbcinemaContext : DbContext
{
    public DbcinemaContext()
    {
    }

    public DbcinemaContext(DbContextOptions<DbcinemaContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Actor> Actors { get; set; }

    public virtual DbSet<Booking> Bookings { get; set; }

    public virtual DbSet<Category> Categories { get; set; }

    public virtual DbSet<Cinema> Cinemas { get; set; }

    public virtual DbSet<Director> Directors { get; set; }

    public virtual DbSet<Hall> Halls { get; set; }

    public virtual DbSet<Movie> Movies { get; set; }

    public virtual DbSet<Schedule> Schedules { get; set; }

    public virtual DbSet<Seat> Seats { get; set; }

    public virtual DbSet<Session> Sessions { get; set; }

    public virtual DbSet<SessionSeat> SessionSeats { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Actor>(entity =>
        {
            entity.Property(e => e.FullName).HasMaxLength(200);

            entity.HasMany(d => d.Movies).WithMany(p => p.Actors)
                .UsingEntity<Dictionary<string, object>>(
                    "MovieActor",
                    r => r.HasOne<Movie>().WithMany()
                        .HasForeignKey("MovieId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MovieActors_Movies"),
                    l => l.HasOne<Actor>().WithMany()
                        .HasForeignKey("ActorId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MovieActors_Actors"),
                    j =>
                    {
                        j.HasKey("ActorId", "MovieId");
                        j.ToTable("MovieActors");
                    });
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.Property(e => e.BookingDate)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.IsPrivateBooking)
                .HasMaxLength(50)
                .IsFixedLength();
            entity.Property(e => e.PrivateBookingPrice).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Session).WithMany(p => p.Bookings)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bookings_Sessions");
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasMany(d => d.Movies).WithMany(p => p.Categories)
                .UsingEntity<Dictionary<string, object>>(
                    "CategoryMovie",
                    r => r.HasOne<Movie>().WithMany()
                        .HasForeignKey("MovieId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_CategoryMovies_Movies1"),
                    l => l.HasOne<Category>().WithMany()
                        .HasForeignKey("CategoryId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_CategoryMovies_Categories"),
                    j =>
                    {
                        j.HasKey("CategoryId", "MovieId").HasName("PK_CategoryMovies_1");
                        j.ToTable("CategoryMovies");
                    });
        });

        modelBuilder.Entity<Cinema>(entity =>
        {
            entity.ToTable("Cinema");

            entity.Property(e => e.Address).HasMaxLength(255);
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Director>(entity =>
        {
            entity.Property(e => e.FullName).HasMaxLength(200);

            entity.HasMany(d => d.Movies).WithMany(p => p.Directors)
                .UsingEntity<Dictionary<string, object>>(
                    "MovieDirector",
                    r => r.HasOne<Movie>().WithMany()
                        .HasForeignKey("MovieId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MovieDirectors_Movies"),
                    l => l.HasOne<Director>().WithMany()
                        .HasForeignKey("DirectorId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MovieDirectors_Directors"),
                    j =>
                    {
                        j.HasKey("DirectorId", "MovieId");
                        j.ToTable("MovieDirectors");
                    });
        });

        modelBuilder.Entity<Hall>(entity =>
        {
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.Cinema).WithMany(p => p.Halls)
                .HasForeignKey(d => d.CinemaId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Halls_Cinema");
        });

        modelBuilder.Entity<Movie>(entity =>
        {
            entity.Property(e => e.Description).HasColumnType("text");
            entity.Property(e => e.Language)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Rating)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Title)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.TrailerLink).HasMaxLength(2050);
        });

        modelBuilder.Entity<Schedule>(entity =>
        {
            entity.Property(e => e.StartTime).HasColumnType("datetime");

            entity.HasOne(d => d.Hall).WithMany(p => p.Schedules)
                .HasForeignKey(d => d.HallId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Schedules_Halls");
        });

        modelBuilder.Entity<Seat>(entity =>
        {
            entity.HasOne(d => d.Hall).WithMany(p => p.Seats)
                .HasForeignKey(d => d.HallId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Seats_Halls");
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.Property(e => e.CreatedAt)
                .IsRowVersion()
                .IsConcurrencyToken();
            entity.Property(e => e.IsActive)
                .HasMaxLength(50)
                .IsFixedLength();

            entity.HasOne(d => d.Movie).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.MovieId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sessions_Movies");

            entity.HasOne(d => d.Schedule).WithMany(p => p.Sessions)
                .HasForeignKey(d => d.ScheduleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Sessions_Schedules");
        });

        modelBuilder.Entity<SessionSeat>(entity =>
        {
            entity.Property(e => e.Price).HasColumnType("decimal(18, 2)");

            entity.HasOne(d => d.Booking).WithMany(p => p.SessionSeats)
                .HasForeignKey(d => d.BookingId)
                .HasConstraintName("FK_SessionSeats_Bookings");

            entity.HasOne(d => d.Seat).WithMany(p => p.SessionSeats)
                .HasForeignKey(d => d.SeatId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SessionSeats_Seats");

            entity.HasOne(d => d.Session).WithMany(p => p.SessionSeats)
                .HasForeignKey(d => d.SessionId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_SessionSeats_Sessions");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
