using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResidentFE
{
    [Serializable]
    public class Reobj
    {
        private IDictionary<String, Object> _campo;

        public Reobj()
        {
            _campo = new Dictionary<String, Object>();
        }

        public Object this[String Chiave]
        {
            get
            {
                if (_campo.ContainsKey(mappaK(Chiave)))
                {
                    return _campo[mappaK(Chiave)];
                }
                else
                {
                    return null;
                }
            }
            set
            {
                _campo[mappaK(Chiave)] = value;
            }
        }

        public ICollection<String> Keys()
        {
            return _campo.Keys;
        }

        
        protected virtual string mappaK(string Chiave)
        {
            return Chiave.ToLower();
        }

        public IDictionary<String, Object> Fields
        {
            get
            {
                return _campo;
            }
        }

      
        public virtual string GetNotNullString(string Campo)
        {
            string mappedKey = mappaK(Campo);

            if (!_campo.ContainsKey(mappedKey) || _campo[mappedKey] == null)
                return String.Empty;

            return _campo[mappedKey].ToString();
        }

        
        public virtual string GetNotNullHtmlString(string Campo)
        {
            string mappedKey = mappaK(Campo);

            if (!_campo.ContainsKey(mappedKey) || _campo[mappedKey] == null)
                return "&nbsp;";

            return _campo[mappedKey].ToString();
        }

 
        public virtual int GetInteger(string Campo)
        {
            string mappedKey = mappaK(Campo);

            if (!_campo.ContainsKey(mappedKey) || _campo[mappedKey] == null)
                return 0;

            return Convert.ToInt32(_campo[mappedKey]);
        }

        public bool GetBool(string Campo)
        {
            return GetBool(Campo, false);
        }


        public virtual bool GetBool(string Campo, bool defaultValue)
        {
            string mappedKey = mappaK(Campo);

            if (!_campo.ContainsKey(mappedKey) || _campo[mappedKey] == null)
                return defaultValue;

            return Convert.ToBoolean(_campo[mappedKey]);
        }

        public virtual double GetDouble(string Campo)
        {
            string mappedKey = mappaK(Campo);

            if (!_campo.ContainsKey(mappedKey) || _campo[mappedKey] == null)
                return 0;

            return Convert.ToDouble(_campo[mappedKey]);
        }

        public virtual decimal GetDecimal(string Campo)
        {
            string mappedKey = mappaK(Campo);

            if (!_campo.ContainsKey(mappedKey) || _campo[mappedKey] == null)
                return 0;

            return Convert.ToDecimal(_campo[mappedKey]);
        }


        public virtual DateTime GetDateTime(string Campo)
        {
            string mappedKey = mappaK(Campo);

            if (!_campo.ContainsKey(mappedKey) || _campo[mappedKey] == null)
                return DateTime.MinValue;

            return Convert.ToDateTime(_campo[mappedKey]);
        }

        public virtual Boolean isnull(string Campo)
        {
            string mappedKey = mappaK(Campo);

            if (!_campo.ContainsKey(mappedKey) || _campo[mappedKey] == null)
                return true;
            if (_campo[mappedKey].ToString() == "")
                return true;
            return false;
        }

        public virtual string GetDateTimeAsString(string Campo, string format)
        {
            string mappedKey = mappaK(Campo);

            if (!_campo.ContainsKey(mappedKey) || _campo[mappedKey] == null)
                return string.Empty;

           // return String.Format("{0:" + format + "}", Convert.ToDateTime(_campo[mappedKey]));
            return String.Format(format, Convert.ToDateTime(_campo[mappedKey]));
        }

    }
}
