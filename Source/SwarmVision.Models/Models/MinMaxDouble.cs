using System;

namespace SwarmVision.HeadPartsTracking.Models
{
    public class MinMaxDouble
    {
        private double _value;

        public virtual double Value
        {
            get { return _value; }
            set
            {
                if (value > Max)
                    _value = Max;

                else if (value < Min)
                    _value = Min;

                else
                    _value = value;
            }    
        }
        public double Min = Double.MinValue;
        public double Max = Double.MaxValue; //By default

        public MinMaxDouble()
        {
            
        }

        public MinMaxDouble(double value, double min, double max)
        {
            if(max < min)
                throw new Exception("Maximum must be greater than minimum");

            Min = min;
            Max = max;
            Value = value;
        }

        public MinMaxDouble Clone()
        {
            return new MinMaxDouble(Value, Min, Max);
        }

        public static implicit operator double (MinMaxDouble minMaxDouble)
        {
            return minMaxDouble.Value;
        }

        public override string ToString()
        {
            return Value.ToString("N3");
        }
    }
}