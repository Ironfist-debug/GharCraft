namespace GharCraft.Domain.Enums;

public enum OrderStatus
{
    PendingPayment = 1,
    Processing = 2,
    Confirmed = 3,
    Shipped = 4,
    OutForDelivery = 5,
    Delivered = 6,
    Cancelled = 7,
    Refunded = 8
}
