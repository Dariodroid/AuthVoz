using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthVoz
{
    internal class AuthV
    {
        public static void AdjustDuration(string file1, string file2)
        {
            // Crear los lectores de archivos de audio
            using (var reader1 = new WaveFileReader(file1))
            using (var reader2 = new WaveFileReader(file2))
            {
                // Obtener la duración de los archivos en segundos
                double duration1 = reader1.TotalTime.TotalSeconds;
                double duration2 = reader2.TotalTime.TotalSeconds;

                // Verificar si las duraciones son diferentes
                if (duration1 != duration2)
                {
                    // Determinar cuál archivo es el más largo y cuál el más corto
                    WaveFileReader longerReader;
                    WaveFileReader shorterReader;
                    string longerFile;
                    string shorterFile;
                    if (duration1 > duration2)
                    {
                        longerReader = reader1;
                        shorterReader = reader2;
                        longerFile = file1;
                        shorterFile = file2;
                    }
                    else
                    {
                        longerReader = reader2;
                        shorterReader = reader1;
                        longerFile = file2;
                        shorterFile = file1;
                    }

                    // Obtener el número de bytes por segundo del archivo más largo
                    int bytesPerSecond = longerReader.WaveFormat.AverageBytesPerSecond;

                    // Calcular la diferencia de duración entre los archivos en segundos
                    double difference = Math.Abs(duration1 - duration2);

                    // Calcular el número de bytes que se deben recortar o agregar al archivo más largo
                    int bytesToAdjust = (int)(difference * bytesPerSecond);

                    // Crear un stream de memoria para guardar los datos del archivo ajustado
                    using (var memoryStream = new MemoryStream())
                    {
                        // Copiar los datos del archivo más largo al stream de memoria
                        longerReader.CopyTo(memoryStream);

                        // Obtener el arreglo de bytes del stream de memoria
                        byte[] buffer = memoryStream.ToArray();

                        // Verificar si se debe recortar o ampliar el archivo más largo
                        if (duration1 > duration2)
                        {
                            // Recortar el arreglo de bytes quitando los bytes sobrantes al final
                            Array.Resize(ref buffer, buffer.Length - bytesToAdjust);
                        }
                        else
                        {
                            // Ampliar el arreglo de bytes agregando bytes cero al final
                            Array.Resize(ref buffer, buffer.Length + bytesToAdjust);
                            for (int i = buffer.Length - bytesToAdjust; i < buffer.Length; i++)
                            {
                                buffer[i] = 0;
                            }
                        }

                        // Crear un nuevo nombre para el archivo ajustado
                        string adjustedFile = Path.GetFileNameWithoutExtension(longerFile) + "_adjusted.wav";

                        // Crear un escritor de archivos de audio con el mismo formato que el archivo más largo
                        using (var writer = new WaveFileWriter(adjustedFile, longerReader.WaveFormat))
                        {
                            // Escribir el arreglo de bytes en el nuevo archivo de audio
                            writer.Write(buffer, 0, buffer.Length);
                        }

                        // Mostrar un mensaje indicando que se ha creado el nuevo archivo de audio ajustado
                        Console.WriteLine("Se ha creado el archivo {0} con la misma duración que {1}", adjustedFile, shorterFile);
                    }
                }
                else
                {
                    // Mostrar un mensaje indicando que los archivos tienen la misma duración y no se necesita ajustarlos
                    Console.WriteLine("Los archivos {0} y {1} tienen la misma duración y no se necesita ajustarlos", file1, file2);
                }
            }
        }
        public static void compara()
        {

            // Ruta de los archivos de audio a comparar
            string file1 = @"C:\S1.wav";
            string file2 = @"C:\S2.wav";

            // Ajustar la duración de los archivos si es necesario
            AdjustDuration(file1, file2);

            // Asignar las rutas de los archivos de audio ajustados si es que se crearon
            if (File.Exists(@"C:\audio1_adjusted.wav"))
            {
                file1 = @"C:\audio1_adjusted.wav";
            }
            if (File.Exists(@"C:\audio2_adjusted.wav"))
            {
                file2 = @"C:\audio2_adjusted.wav";
            }

            // Crear los lectores de archivos de audio
            using (var reader1 = new WaveFileReader(file1))
            using (var reader2 = new WaveFileReader(file2))
            {
                // Convertir los archivos a formato PCM 16 bits mono
                var waveFormat = new WaveFormat(16000, 16, 1);
                using (var converter1 = new WaveFormatConversionStream(waveFormat, reader1))
                using (var converter2 = new WaveFormatConversionStream(waveFormat, reader2))
                {
                    // Crear los agregadores de muestras
                    var aggregator1 = new SampleAggregator(converter1);
                    var aggregator2 = new SampleAggregator(converter2);

                    // Calcular la correlación cruzada entre las muestras
                    double correlation = aggregator1.Correlate(aggregator2);

                    // Mostrar el resultado
                    Console.WriteLine("La correlación cruzada entre los archivos es: {0}", Math.Round(correlation, 3));
                }
            }

        }

        // Clase que calcula la correlación cruzada entre dos señales de audio
        public class SampleAggregator
        {
            private float[] samples; // Arreglo que almacena las muestras de audio
            private int sampleCount; // Contador de muestras

            // Constructor que recibe un stream de audio y lee sus muestras
            public SampleAggregator(WaveStream stream)
            {
                // Obtener el número total de muestras del stream
                this.sampleCount = (int)(stream.Length / stream.BlockAlign);

                // Crear el arreglo de muestras con el tamaño adecuado
                samples = new float[sampleCount];

                // Crear un lector de muestras a partir del stream
                var sampleProvider = stream.ToSampleProvider();

                // Leer las muestras del stream y almacenarlas en el arreglo
                sampleProvider.Read(samples, 0, sampleCount);
            }

            // Método que calcula la correlación cruzada entre esta señal y otra señal
            public double Correlate(SampleAggregator other)
            {
                // Asegurarse que ambas señales tengan el mismo número de muestras
                //if (this.sampleCount != other.sampleCount)
                //    throw new ArgumentException("Las señales deben tener el mismo número de muestras");

                // Inicializar la suma de productos y las sumas de cuadrados
                double sumXY = 0;
                double sumX2 = 0;
                double sumY2 = 0;

                // Recorrer las muestras de ambas señales hasta el menor de los dos tamaños
                for (int i = 0; i < Math.Min(this.sampleCount, other.sampleCount); i++)
                {
                    // Obtener las muestras correspondientes
                    float x = this.samples[i];
                    float y = other.samples[i];

                    // Actualizar la suma de productos y las sumas de cuadrados
                    sumXY += x * y;
                    sumX2 += x * x;
                    sumY2 += y * y;
                }


                // Calcular la correlación cruzada usando la fórmula matemática
                double correlation = sumXY / Math.Sqrt(sumX2 * sumY2);

                // Devolver el resultado
                return correlation;
            }
        }

    }
}
