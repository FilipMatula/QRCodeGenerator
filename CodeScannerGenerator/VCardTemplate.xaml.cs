using CodeScannerGenerator.Common;
using MixERP.Net.VCards;
using MixERP.Net.VCards.Models;
using MixERP.Net.VCards.Serializer;
using MixERP.Net.VCards.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CodeScannerGenerator
{
    /// <summary>
    /// Interaction logic for VCardTemplate.xaml
    /// </summary>
    public partial class VCardTemplate : Window
    {
        public bool IsReadonly { get; set; }
        public Visibility IsCancelVisible { get { return IsReadonly ? Visibility.Collapsed : Visibility.Visible; } }

        public VCardTemplate(string culture, bool readOnly = false)
        {
            InitializeComponent();
            this.DataContext = this;
            LocUtil.SwitchLanguage(this, culture);
            IsReadonly = readOnly;
        }

        private void btnDialogOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        public string GetVCardText()
        {
            try
            {
                var vcard = new VCard
                {
                    Version = VCardVersion.V4,
                    Title = Title.Text,
                    FirstName = FirstName.Text,
                    LastName = LastName.Text,
                    FormattedName = $"{FirstName.Text} {LastName.Text}",
                    Suffix = Suffix.Text,
                    Organization = Company.Text,
                    Addresses = new List<Address> {
                        new Address
                        {
                            Type = AddressType.Home,
                            Street = Street.Text,
                            Locality = City.Text,
                            Country = Country.Text,
                            PostalCode = Zip.Text
                        }
                    },
                    Telephones = new List<Telephone>
                    {
                        new Telephone
                        {
                            Type = TelephoneType.Home,
                            Number = Phone.Text
                        },
                        new Telephone
                        {
                            Type = TelephoneType.Cell,
                            Number = Mobile.Text
                        },
                        new Telephone
                        {
                            Type = TelephoneType.Fax,
                            Number = Fax.Text
                        }
                    },
                    Emails = new List<Email>
                    {
                        new Email
                        {
                            EmailAddress = Email.Text
                        }
                    },
                    Role = JobTitle.Text
                };

                if (!string.IsNullOrEmpty(Website.Text))
                {
                    UriBuilder uriBuilder = new UriBuilder(Website.Text);
                    uriBuilder.Scheme = "https";
                    if (uriBuilder.Port == 80)
                        uriBuilder.Port = -1;
                    vcard.Url = uriBuilder.Uri;
                }

                return vcard.Serialize();
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message, LocUtil.TranslatedString("Error", this));
            }
            return "";
        }

        public void SetVCardText(string text)
        {
            VCard vcard = Deserializer.GetVCard(text);
            if (vcard.Url != null)
            {
                UriBuilder uriBuilder = new UriBuilder(vcard.Url);
                if (uriBuilder.Port == 80)
                    uriBuilder.Port = -1;
                Website.Text = uriBuilder.Uri.ToString();
            }
            Title.Text = vcard.Title;
            FirstName.Text = vcard.FirstName;
            LastName.Text = vcard.LastName;
            Suffix.Text = vcard.Suffix;
            Company.Text = vcard.Organization;
            JobTitle.Text = vcard.Role;
            if (vcard.Emails != null)
                Email.Text = string.Join("; ", vcard.Emails.Select(e => e.EmailAddress));
            if (vcard.Telephones != null)
            {
                Mobile.Text = string.Join("; ", vcard.Telephones.Where(t => t.Type == TelephoneType.Cell).Select(t => t.Number));
                Phone.Text = string.Join("; ", vcard.Telephones.Where(t => t.Type == TelephoneType.Home).Select(t => t.Number));
                Fax.Text = string.Join("; ", vcard.Telephones.Where(t => t.Type == TelephoneType.Fax).Select(t => t.Number));
            }
            if (vcard.Addresses != null)
            {
                Street.Text = vcard.Addresses.Where(t => t.Type == AddressType.Home).Select(t => t.Street).FirstOrDefault();
                City.Text = vcard.Addresses.Where(t => t.Type == AddressType.Home).Select(t => t.Locality).FirstOrDefault();
                Country.Text = vcard.Addresses.Where(t => t.Type == AddressType.Home).Select(t => t.Country).FirstOrDefault();
                Zip.Text = vcard.Addresses.Where(t => t.Type == AddressType.Home).Select(t => t.PostalCode).FirstOrDefault();
            }
        }
    }
}
