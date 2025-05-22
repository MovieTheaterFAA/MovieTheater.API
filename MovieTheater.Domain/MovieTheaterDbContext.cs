using MovieTheater.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MovieTheater.Domain
{
    public class MovieTheaterDbContext : DbContext
    {
        public MovieTheaterDbContext() { }

        public MovieTheaterDbContext(DbContextOptions<MovieTheaterDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<CinemaRoom> CinemaRooms { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<ShowTime> Showtimes { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<BookingSeat> BookingSeats { get; set; }
        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<TicketSeat> TicketSeats { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<Promotion> Promotions { get; set; }
        public DbSet<ScoreHistory> ScoreHistory { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Seat ↔ CinemaRoom (many-to-one)
            modelBuilder.Entity<Seat>()
                .HasOne(s => s.CinemaRoom)
                .WithMany(cr => cr.Seats)
                .HasForeignKey(s => s.CinemaRoomId);

            // Showtime ↔ Movie (many-to-one)
            modelBuilder.Entity<ShowTime>()
                .HasOne(st => st.Movie)
                .WithMany(m => m.Showtimes)
                .HasForeignKey(st => st.MovieId);

            // Showtime ↔ CinemaRoom (many-to-one)
            modelBuilder.Entity<ShowTime>()
                .HasOne(st => st.CinemaRoom)
                .WithMany(cr => cr.Showtimes)
                .HasForeignKey(st => st.CinemaRoomId);

            // Booking ↔ User (member) (many-to-one)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Member)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.MemberId);

            // Booking ↔ Showtime (many-to-one)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Showtime)
                .WithMany(st => st.Bookings)
                .HasForeignKey(b => b.ShowtimeId);

            // BookingSeat composite key and relationships
            modelBuilder.Entity<BookingSeat>()
                .HasKey(bs => new { bs.BookingId, bs.SeatId });

            modelBuilder.Entity<BookingSeat>()
                .HasOne(bs => bs.Booking)
                .WithMany(b => b.BookingSeats)
                .HasForeignKey(bs => bs.BookingId);

            modelBuilder.Entity<BookingSeat>()
                .HasOne(bs => bs.Seat)
                .WithMany(s => s.BookingSeats)
                .HasForeignKey(bs => bs.SeatId);

            // Ticket ↔ Booking (many-to-one)
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Booking)
                .WithMany(b => b.Tickets)
                .HasForeignKey(t => t.BookingId);

            // TicketSeat composite key and relationships
            modelBuilder.Entity<TicketSeat>()
                .HasKey(ts => new { ts.TicketId, ts.SeatId });

            modelBuilder.Entity<TicketSeat>()
                .HasOne(ts => ts.Ticket)
                .WithMany(t => t.TicketSeats)
                .HasForeignKey(ts => ts.TicketId);

            modelBuilder.Entity<TicketSeat>()
                .HasOne(ts => ts.Seat)
                .WithMany(s => s.TicketSeats)
                .HasForeignKey(ts => ts.SeatId);

            // Invoice ↔ Booking (one-to-one)
            modelBuilder.Entity<Invoice>()
                .HasOne(i => i.Booking)
                .WithOne(b => b.Invoice)
                .HasForeignKey<Invoice>(i => i.BookingId);

            // Payment ↔ Invoice (many-to-one)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Invoice)
                .WithMany(i => i.Payments)
                .HasForeignKey(p => p.InvoiceId);

            // ScoreHistory ↔ User (many-to-one)
            modelBuilder.Entity<ScoreHistory>()
                .HasOne(sh => sh.Member)
                .WithMany(u => u.ScoreHistory)
                .HasForeignKey(sh => sh.MemberId);

            // ScoreHistory ↔ Booking (many-to-one, optional)
            modelBuilder.Entity<ScoreHistory>()
                .HasOne(sh => sh.RelatedBooking)
                .WithMany(b => b.ScoreHistories)
                .HasForeignKey(sh => sh.RelatedBookingId)
                .OnDelete(DeleteBehavior.SetNull);

            // Promotions: standalone, no relationships
        }
    }
}
