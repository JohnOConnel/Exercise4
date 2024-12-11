using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlightReservation
{
    public class ReservationNode
    {
        // Fields to store reservation details
        public string PassengerName;           // Name of the passenger
        public string FlightNumber;            // Flight number associated with the reservation
        public string SeatNumber;              // Assigned seat number
        public DateTime BookingDate;           // Date of booking
        public int ReservationID;              // Unique identifier for each reservation

        // Pointers for doubly linked list structure
        public ReservationNode next;
        public ReservationNode prev;

        // Constructor to initialize a reservation with provided details
        public ReservationNode(string name, string flightNumber, string seatNumber, DateTime bookingDate, int reservationID)
        {
            PassengerName = name;
            FlightNumber = flightNumber;
            SeatNumber = seatNumber;
            BookingDate = bookingDate;
            ReservationID = reservationID;
            next = null;
            prev = null;
        }
    }

    public class ReservationList
    {
        private ReservationNode head;                      // Head node of the main reservations list
        private ReservationNode urgentHead;                // Separate head node for urgent reservations
        private const string filePath = @"C:\ReservationData\reservations.txt"; // Path to save reservations

        // Dictionary to manage seat availability for each flight
        private Dictionary<string, HashSet<string>> seatAvailability = new Dictionary<string, HashSet<string>>();

        private static readonly object fileLock = new object();

        private bool isLoading = false;

        // Constructor initializes the list and loads data from file if available
        public ReservationList()
        {
            head = null;
            urgentHead = null;
            LoadReservationsFromFile(); // Load existing data at startup
        }

        // Adds a new reservation in sorted order based on booking date
        public void AddReservation(string name, string flightNumber, string seatNumber, DateTime bookingDate, int reservationID)
        {
            ReservationNode newNode = new ReservationNode(name, flightNumber, seatNumber, bookingDate, reservationID);

            // If list is empty or booking date is the earliest, add at the head
            if (head == null || head.BookingDate > newNode.BookingDate)
            {
                newNode.next = head;
                if (head != null) head.prev = newNode;
                head = newNode;
            }
            else
            {
                // Insert in the correct sorted position by booking date
                ReservationNode current = head;
                while (current.next != null && current.next.BookingDate < newNode.BookingDate)
                {
                    current = current.next;
                }
                newNode.next = current.next;
                if (current.next != null) current.next.prev = newNode;
                current.next = newNode;
                newNode.prev = current;
            }

            Console.WriteLine($"Reservation added for {name} on flight {flightNumber}.");
            SaveReservationsToFile(); // Persist new reservation to file
        }

        // Removes a reservation identified by its unique reservation ID
        public void RemoveReservation(int reservationID)
        {
            if (head == null)
            {
                Console.WriteLine("No reservations to remove.");
                return;
            }

            // Check if the head reservation matches the ID
            if (head.ReservationID == reservationID)
            {
                head = head.next;
                if (head != null) head.prev = null;
                Console.WriteLine($"Reservation ID {reservationID} has been removed.");
                SaveReservationsToFile(); // Save updated list to file
                return;
            }

            // Traverse the list to find and remove the reservation by ID
            ReservationNode current = head;
            while (current != null && current.ReservationID != reservationID)
            {
                current = current.next;
            }

            if (current != null)
            {
                current.prev.next = current.next;
                if (current.next != null) current.next.prev = current.prev;
                Console.WriteLine($"Reservation ID {reservationID} has been removed.");
                SaveReservationsToFile(); // Persist changes to file
            }
            else
            {
                Console.WriteLine($"No reservation found with ID {reservationID}.");
            }
        }

        // Prints all reservations in sorted order by booking date
        public void PrintReservations()
        {
            if (urgentHead == null && head == null)
            {
                Console.WriteLine("No reservations found.");
                return;
            }

            Console.WriteLine("Current Reservations (sorted by booking date):");

            // Display urgent reservations first
            if (urgentHead != null)
            {
                Console.WriteLine("Urgent Reservations (within 24 hours):");
                ReservationNode currentUrgent = urgentHead;
                while (currentUrgent != null)
                {
                    Console.WriteLine($"[URGENT] Passenger: {currentUrgent.PassengerName}, Flight: {currentUrgent.FlightNumber}, Seat: {currentUrgent.SeatNumber}, Date: {currentUrgent.BookingDate.ToShortDateString()}, ID: {currentUrgent.ReservationID}");
                    currentUrgent = currentUrgent.next;
                }
            }

            // Display regular reservations
            if (head != null)
            {
                Console.WriteLine("Regular Reservations:");
                ReservationNode current = head;
                while (current != null)
                {
                    Console.WriteLine($"Passenger: {current.PassengerName}, Flight: {current.FlightNumber}, Seat: {current.SeatNumber}, Date: {current.BookingDate.ToShortDateString()}, ID: {current.ReservationID}");
                    current = current.next;
                }
            }
        }

        // Searches reservations by flight number and booking date
        public void SearchReservations(string flightNumber, DateTime bookingDate)
        {
            if (head == null)
            {
                Console.WriteLine("No reservations to search.");
                return;
            }

            bool found = false;
            ReservationNode current = head;
            Console.WriteLine($"Searching for reservations on flight {flightNumber} for date {bookingDate.ToShortDateString()}...");
            while (current != null)
            {
                if (current.FlightNumber == flightNumber && current.BookingDate.Date == bookingDate.Date)
                {
                    Console.WriteLine($"Found: Passenger {current.PassengerName}, Seat {current.SeatNumber}, Reservation ID {current.ReservationID}");
                    found = true;
                }
                current = current.next;
            }

            if (!found) Console.WriteLine("No matching reservations found.");
        }

        // Saves all reservations to a text file for persistence
        private void SaveReservationsToFile()
        {
            if (isLoading) return; // Prevent saving during loading

            try
            {
                using (StreamWriter writer = new StreamWriter(filePath, false)) // Overwrite file
                {
                    // Save regular reservations
                    ReservationNode current = head;
                    while (current != null)
                    {
                        writer.WriteLine($"{current.PassengerName},{current.FlightNumber},{current.SeatNumber},{current.BookingDate.ToString("o")},{current.ReservationID},regular");
                        current = current.next;
                    }

                    // Save urgent reservations
                    ReservationNode urgentCurrent = urgentHead;
                    while (urgentCurrent != null)
                    {
                        writer.WriteLine($"{urgentCurrent.PassengerName},{urgentCurrent.FlightNumber},{urgentCurrent.SeatNumber},{urgentCurrent.BookingDate.ToString("o")},{urgentCurrent.ReservationID},urgent");
                        urgentCurrent = urgentCurrent.next;
                    }
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine("Error saving reservations: " + ex.Message);
            }
        }




        // Loads reservations from the file into the list at startup
        private void LoadReservationsFromFile()
        {
            isLoading = true; // Set loading flag to prevent accidental saving during load

            if (File.Exists(filePath))
            {
                try
                {
                    using (StreamReader reader = new StreamReader(filePath))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            string[] details = line.Split(',');
                            string name = details[0];
                            string flightNumber = details[1];
                            string seatNumber = details[2];
                            DateTime bookingDate = DateTime.Parse(details[3], null, DateTimeStyles.RoundtripKind);
                            int reservationID = int.Parse(details[4]);
                            string type = details[5]; // "regular" or "urgent"

                            // Add to the correct list based on the reservation type
                            if (type == "urgent")
                            {
                                AddUrgentReservation(name, flightNumber, seatNumber, bookingDate, reservationID);
                            }
                            else
                            {
                                AddReservation(name, flightNumber, seatNumber, bookingDate, reservationID);
                            }
                        }
                    }
                }
                catch (IOException ex)
                {
                    Console.WriteLine("Error loading reservations: " + ex.Message);
                }
            }

            isLoading = false; // Reset loading flag
        }




        // TODO: Implement Priority Queue for Urgent Reservations (Option 1)
        public void AddUrgentReservation(string name, string flightNumber, string seatNumber, DateTime bookingDate, int reservationID)
        {
            // TODO: Add logic here to add urgent reservations in sorted order by booking date
            // Check if the reservation is urgent (within the next 24 hours)

            //   Y  O  U  R    C  O  D  E    G  O  E  S    H  E  R  E  .  .  .  
        }

        // TODO: Implement Binary Search for Fast Flight Search (Option 2)
        public void SearchReservationsBinary(string flightNumber)
        {
            // TODO: Implement binary search for fast retrieval of flight reservations
            // Convert linked list to list for binary search

            //   Y  O  U  R    C  O  D  E    G  O  E  S    H  E  R  E  .  .  .  

        }

        // TODO: Implement Hash Table for Seat Management (Option 3)
        public void ManageSeatAvailability(string flightNumber, string seatNumber, bool isAvailable)
        {
            // TODO: Manage seat availability with a hash table, add/remove seats based on availability status
            // Initialize the flight's seat collection if it doesn't exist

            //   Y  O  U  R    C  O  D  E    G  O  E  S    H  E  R  E  .  .  .  

        }

        // TODO: Implement Merge Sort for Sorting Reservations (Option 4)
        public void SortReservationsByDate()
        {
            // TODO: Implement merge sort to sort reservations by booking date
            // Recursively divide the list, sort, and merge in ascending booking date order

            //   Y  O  U  R    C  O  D  E    G  O  E  S    H  E  R  E  .  .  .  
        }



        class Program
        {
            static void Main(string[] args)
            {
                ReservationList reservationList = new ReservationList();
                bool running = true;

                while (running)
                {
                    Console.WriteLine("\n--- Reservation System Menu ---");
                    Console.WriteLine("A. Add a Reservation");
                    Console.WriteLine("B. Remove a Reservation");
                    Console.WriteLine("C. Print All Reservations");
                    Console.WriteLine("D. Search Reservations by Flight and Date");
                    Console.WriteLine("1. Add an Urgent Reservation (Priority Queue)");
                    Console.WriteLine("2. Fast Search for Specific Reservations (Binary Search)");
                    Console.WriteLine("3. Manage Seat Availability (Hash Table)");
                    Console.WriteLine("4. Sort Reservations by Date (Merge Sort)");
                    Console.WriteLine("X. Exit");
                    Console.Write("Select an option: ");

                    string choice = Console.ReadLine();
                    switch (choice.ToUpper())
                    {
                        case "A":
                            ManualDataEntry(reservationList);
                            break;

                        case "B":
                            RemoveReservation(reservationList);
                            break;

                        case "C":
                            reservationList.PrintReservations();
                            break;

                        case "D":
                            SearchReservation(reservationList);
                            break;

                        case "1":
                            // TODO - You must prompt for details and call AddUrgentReservation for urgent reservations
                            Console.WriteLine("Urgent reservation functionality not yet implemented.");

                            // Replace the 'Console.WriteLine("Urgent reservation functionality not yet implemented.")' with your method call to Option 1
                            //   Y  O  U  R    O  P  T  I  O  N    1    M  E  T  H  O  D    C  A  L  L    G  O  E  S    H  E  R  E  .  .  .  

                            break;

                        case "2":
                            // TODO - You must call SearchReservationsBinary for a fast search by flight number
                            Console.WriteLine("Binary search functionality not yet implemented.");


                            // Replace the 'Console.WriteLine("Binary search functionality not yet implemented.")' with your method call to Option 2
                            //   Y  O  U  R    O  P  T  I  O  N    2    M  E  T  H  O  D    C  A  L  L    G  O  E  S    H  E  R  E  .  .  . 

                            break;

                        case "3":
                            // TODO - You must call ManageSeatAvailability for seat management
                            Console.WriteLine("Seat management functionality not yet implemented.");


                            // Replace the 'Console.WriteLine("Seat management functionality not yet implemented.")' with your method call to Option 3
                            //   Y  O  U  R    O  P  T  I  O  N    3    M  E  T  H  O  D    C  A  L  L    G  O  E  S    H  E  R  E  .  .  . 

                            break;

                        case "4":
                            // TODO - You must call SortReservationsByDate to sort reservations by booking date
                            Console.WriteLine("Sorting functionality not yet implemented.");


                            // Replace the 'Console.WriteLine("Sorting functionality not yet implemented.")' with your method call to Option 4
                            //   Y  O  U  R    O  P  T  I  O  N    4    M  E  T  H  O  D    C  A  L  L    G  O  E  S    H  E  R  E  .  .  . 


                            break;

                        case "X":
                            Console.WriteLine("Exiting program. Goodbye!");
                            running = false;
                            break;

                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }
                }
            }

            static void ManualDataEntry(ReservationList reservationList)
            {
                Console.Write("Enter Passenger Name: ");
                string name = Console.ReadLine();
                Console.Write("Enter Flight Number: ");
                string flightNumber = Console.ReadLine();
                Console.Write("Enter Seat Number: ");
                string seatNumber = Console.ReadLine();
                Console.Write("Enter Booking Date (yyyy-mm-dd): ");
                DateTime bookingDate = DateTime.Parse(Console.ReadLine());
                Console.Write("Enter Reservation ID: ");
                int reservationID = int.Parse(Console.ReadLine());

                reservationList.AddReservation(name, flightNumber, seatNumber, bookingDate, reservationID);
                Console.WriteLine("Reservation added manually.");
            }

            static void RemoveReservation(ReservationList reservationList)
            {
                Console.Write("Enter Reservation ID to Remove: ");
                int removeID = int.Parse(Console.ReadLine());
                reservationList.RemoveReservation(removeID);
            }

            static void SearchReservation(ReservationList reservationList)
            {
                Console.Write("Enter Flight Number to Search: ");
                string searchFlight = Console.ReadLine();
                Console.Write("Enter Booking Date (yyyy-mm-dd) to Search: ");
                DateTime searchDate = DateTime.Parse(Console.ReadLine());
                reservationList.SearchReservations(searchFlight, searchDate);
            }
        }

    }
}
