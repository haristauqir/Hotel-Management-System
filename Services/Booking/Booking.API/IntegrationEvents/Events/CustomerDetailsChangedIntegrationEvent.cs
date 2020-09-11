using Foundation.EventBus.Events;

namespace Booking.API.IntegrationEvents.Events
{
    public class CustomerDetailsChangedIntegrationEvent : IntegrationEvent
    {
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string CustomerPhone { get; set; }

        public CustomerDetailsChangedIntegrationEvent(int customerId, string customerName, string customerPhone)
        {
            CustomerId = customerId;
            CustomerName = customerName;
            CustomerPhone = customerPhone;
        }
    }
}