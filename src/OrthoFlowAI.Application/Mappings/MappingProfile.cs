using AutoMapper;
using OrthoFlowAI.Application.DTOs;
using OrthoFlowAI.Domain.Entities;

namespace OrthoFlowAI.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<CreateClinicRequest, Clinic>();
        CreateMap<UpdateClinicRequest, Clinic>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));
        CreateMap<Clinic, ClinicResponse>();
        CreateMap<ClinicSettingsRequest, Clinic>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<CreatePatientRequest, Patient>();
        CreateMap<UpdatePatientRequest, Patient>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Patient, PatientResponse>()
            .ForMember(dest => dest.DoctorName,
                opt => opt.MapFrom(src => src.Doctor != null
                    ? $"{src.Doctor.User.FirstName} {src.Doctor.User.LastName}"
                    : null))
            .ForMember(dest => dest.BranchName,
                opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : null));

        CreateMap<Patient, PatientListResponse>()
            .ForMember(dest => dest.DoctorName,
                opt => opt.MapFrom(src => src.Doctor != null
                    ? $"{src.Doctor.User.FirstName} {src.Doctor.User.LastName}"
                    : null))
            .ForMember(dest => dest.BranchName,
                opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : null));

        CreateMap<CreateAppointmentRequest, Appointment>();
        CreateMap<UpdateAppointmentRequest, Appointment>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Appointment, AppointmentResponse>()
            .ForMember(dest => dest.PatientName,
                opt => opt.MapFrom(src =>
                    $"{src.Patient.FirstName} {src.Patient.LastName}"))
            .ForMember(dest => dest.DoctorName,
                opt => opt.MapFrom(src =>
                    $"{src.Doctor.User.FirstName} {src.Doctor.User.LastName}"))
            .ForMember(dest => dest.BranchName,
                opt => opt.MapFrom(src => src.Branch != null ? src.Branch.Name : null));

        CreateMap<CreateInvoiceRequest, Invoice>();
        CreateMap<UpdateInvoiceRequest, Invoice>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<Invoice, InvoiceResponse>()
            .ForMember(dest => dest.PatientName,
                opt => opt.MapFrom(src =>
                    $"{src.Patient.FirstName} {src.Patient.LastName}"))
            .ForMember(dest => dest.DoctorName,
                opt => opt.MapFrom(src => src.Doctor != null
                    ? $"{src.Doctor.User.FirstName} {src.Doctor.User.LastName}"
                    : null));

        CreateMap<CreatePaymentRequest, Payment>();
        CreateMap<Payment, PaymentResponse>();

        CreateMap<UploadPhotoRequest, Photo>();
        CreateMap<Photo, PhotoResponse>();

        CreateMap<Notification, NotificationResponse>();

        CreateMap<CreateTreatmentVisitRequest, TreatmentVisit>();
        CreateMap<TreatmentVisit, TreatmentVisitResponse>()
            .ForMember(dest => dest.PatientName,
                opt => opt.MapFrom(src =>
                    $"{src.Patient.FirstName} {src.Patient.LastName}"))
            .ForMember(dest => dest.DoctorName,
                opt => opt.MapFrom(src =>
                    $"{src.Doctor.User.FirstName} {src.Doctor.User.LastName}"));

        CreateMap<ApplicationUser, AuthUserInfo>();
    }
}
