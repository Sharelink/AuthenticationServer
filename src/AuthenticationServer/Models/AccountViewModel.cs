using Microsoft.AspNet.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace AuthenticationServer.Models
{
    public class LoginViewModel
    {
        [Required]
        [LoginString]
        [Display(Name = "LoginString")]
        public string LoginString { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public class AccountIDAttribute : DataTypeAttribute
    {
        public AccountIDAttribute():base("AccountID")
        {
            
        }

        public override bool IsValid(object value)
        {
            try
            {
                return long.Parse(value.ToString()) > 147258;
            }
            catch (System.Exception)
            {
                return false;
            }
            
        }
    }

    public class LoginStringAttribute : DataTypeAttribute
    {
        private static EmailAddressAttribute EmailValidate = new EmailAddressAttribute();
        private static PhoneAttribute PhoneValidate = new PhoneAttribute();
        private static AccountIDAttribute AccountIDValidate = new AccountIDAttribute();
        public LoginStringAttribute():base("LoginString")
        {
            
        }

        public override bool IsValid(object value)
        {
            return new EmailAddressAttribute().IsValid(value) || PhoneValidate.IsValid(value) || AccountIDValidate.IsValid(value);
        }
    }
}
