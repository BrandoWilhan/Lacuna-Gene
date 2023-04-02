using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace LacunaGene
{
    class Program                                       
    {
    
        static async Task Main()
        {

            //job requesting
               var access = await Logar();
               var token = ToResponseToken(access).accessToken; //deserializing token response
               var jobJson = await JobRequest(token);
               var jobResponse = ToResponseJob(jobJson);
               Console.WriteLine(jobResponse.job.type);
               Console.WriteLine(jobResponse.job.strandEncoded + "\n");
               Console.WriteLine(jobResponse.job.strand + "\n");
               Console.WriteLine(jobResponse.job.geneEncoded + "\n");

               if (jobResponse.job.type == "DecodeStrand")
               {
                   var strand = StrandDecode(jobResponse.job.strandEncoded);
                   var bodyDecode = new { strand };
                   var response = PostDecode(bodyDecode, jobResponse.job.id, token);
                   Console.WriteLine(strand);
               }

               if (jobResponse.job.type == "EncodeStrand")
               {
                   var strandEncoded = StrandEncode(jobResponse.job.strand);
                   var bodyEncode = new { strandEncoded };
                   var response = PostDecode(bodyEncode, jobResponse.job.id, token);
                   Console.WriteLine(strandEncoded+"\n");
               }

               if (jobResponse.job.type == "CheckGene")
               {
                   var isActivated = CheckGene(jobResponse.job.geneEncoded, jobResponse.job.strandEncoded);
                   var bodyCheck = new { isActivated };
                   var response = PostDecode(bodyCheck, jobResponse.job.id, token);
                   Console.WriteLine(isActivated);
               }
                                    
        }


        public static async Task<string> JobRequest(string token)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var response = await httpClient.GetAsync("https://gene.lacuna.cc/api/dna/jobs");
            var data = response.Content.ReadAsStringAsync().Result;
            return data;
        }

        public static async Task<string> Logar()
        {
            var httpClient = new HttpClient();

            Console.WriteLine("Please enter your username: ");
            var username = Console.ReadLine();
            Console.WriteLine("Please enter your password: ");
            var password = Console.ReadLine();

            var user = new { username = "mugen", password = "Bw321147" };
            var content = ToRequest(user);
            var response = await httpClient.PostAsync("https://gene.lacuna.cc/api/users/login", content);
            var data = response.Content.ReadAsStringAsync().Result;
            return data;
        }

        public static async Task<string> PostDecode(object bodyDecode, string id, string token)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var content = ToRequest(bodyDecode);
            var response = await httpClient.PostAsync("https://gene.lacuna.cc/api/dna/jobs/{id}/decode", content);
            var data = response.Content.ReadAsStringAsync().Result;
            return data;
        }

        public static async Task<string> PostEncode(object bodyEncode, string id, string token)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var content = ToRequest(bodyEncode);
            var response = await httpClient.PostAsync("https://gene.lacuna.cc/api/dna/jobs/{id}/encode", content);
            var data = response.Content.ReadAsStringAsync().Result;
            return data;
        }

        public static async Task<string> PostCheck(object bodyCheck, string id, string token)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var content = ToRequest(bodyCheck);
            var response = await httpClient.PostAsync("https://gene.lacuna.cc/api/dna/jobs/{id}/gene", content);
            var data = response.Content.ReadAsStringAsync().Result;
            return data;
        }

        public static async Task CriarUsuario()
        {
            var httpClient = new HttpClient();
            var userCreate = new { username = "mugen", email = "brandowilhan@gmail.com", password = "*******" };
            var content = ToRequest(obj: userCreate);
            await httpClient.PostAsync("https://gene.lacuna.cc/api/users/create", content);
        }
    
        private static StringContent ToRequest(object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            var data = new StringContent(json, Encoding.UTF8, "application/json");
            return data;
        }

        private static ResponseToken ToResponseToken(string json)
        {
            var data = JsonConvert.DeserializeObject<ResponseToken>(json);
            return data;
        }

        private static ResponseJob ToResponseJob(string json)
        {
            var data = JsonConvert.DeserializeObject<ResponseJob>(json);
            return data;
        }

        public static string StrandEncode(string strand) //encoding
        {
            var binaryStrand = StrandToBinary(strand);   //strand CAT - > 010010
            var byteStrand = BinToByte(binaryStrand);  // 0b -> [0b, 0b]
            var base64Strand = Convert.ToBase64String(byteStrand); //[0b, 0b] -> 064
            return base64Strand;
        }

        public static string StrandDecode(string base64Strand)
        {
            var strandByte = Base64ToByte(base64Strand);  // 064 -> [0b, 0b]
            var binStrand = ByteToBin(strandByte);   // [0b, 0b] -> 0b          
            var strand = BinaryToStrand(binStrand);    // 0b -> strand CAT  
            if(!(IsMainStrand(strand)))
                strand = ReverseStrand(strand);
            return strand;
        }

        public static bool CheckGene(string geneEncoded, string strandEncoded)
        {
            bool isActivated = false;
            string check;

            for(int i = 0; i < (geneEncoded.Length/2); i++)
            {
                check = geneEncoded.Substring(i, (geneEncoded.Length / 2));
                if (strandEncoded.Contains(check))
                {
                    isActivated = true;
                    break;
                }
            }
            return isActivated;
        }

        public static string BinaryToStrand(string strandBinary)
        {
            string strand = "";
            string aux;
            
            for (int i = 0; i < ((strandBinary.Length)/2); i++)
            {
                aux = strandBinary.Substring(2 * i, 2);

                if (aux == "00")
                    strand += "A";

                if (aux == "01")
                    strand += "C";

                if (aux == "10")
                    strand += "G";

                if (aux == "11")
                    strand += "T";
            }
            return strand;
        }

        public static string StrandToBinary(string strand)
        {
            var strandEncodedBinary = "";
            
            if(IsMainStrand(strand))
            {
                foreach(char c in strand)
                {
                    if(c.Equals('A'))
                    {
                        strandEncodedBinary += "00";
                    }
                    
                    if (c.Equals('C'))
                    {
                        strandEncodedBinary += "01";
                    }
                    
                    if (c.Equals('G'))
                    {
                        strandEncodedBinary += "10";
                    }
                    
                    if (c.Equals('T'))
                    {
                        strandEncodedBinary += "11";
                    }
                }
                    
            }
            else
            {
                var strandReverse = ReverseStrand(strand);
                
                foreach (char c in strandReverse)
                {
                    if (c.Equals('A'))
                    {
                        strandEncodedBinary += "00";
                    }

                    if (c.Equals('C'))
                    {
                        strandEncodedBinary += "01";
                    }

                    if (c.Equals('G'))
                    {
                        strandEncodedBinary += "10";
                    }

                    if (c.Equals('T'))
                    {
                        strandEncodedBinary += "11";
                    }
                }
            }
            return strandEncodedBinary;
        }

        public static byte[] BinToByte(string strandBin)
        {
            var byteStrand = new byte[strandBin.Length / 8];
            for (var i = 0; i < byteStrand.Length; i++)
            {
                byteStrand[i] = Convert.ToByte(strandBin.Substring(i * 8, 8), 2);
            }
            return byteStrand; 
        }

        public static string ByteToBin(byte[] strandByte)
        {
            var strandBin = "";

            for (int i = 0; i < strandByte.Length; i++)
            {
                strandBin += Convert.ToString(strandByte[i], 2).PadLeft(8, '0'); 
            }
            
            if(strandBin.Length%2 != 0)
                strandBin = "0" + strandBin;
            return strandBin;
        }

        public static byte[] Base64ToByte(string strand64)
        {
            byte[] bytesStrand = Convert.FromBase64String(strand64);
            return bytesStrand;
        }

        public static Boolean IsMainStrand(string strand)
        {
            if (strand[0] == 'C' && strand[1] == 'A' && strand[2] == 'T')
                return true;
            return false;
        }

        public static string ReverseStrand(string strand)
        {
            StringBuilder strandBuilder = new StringBuilder(strand);
            for(int i = 0; i < strand.Length; i++)
            {
                if (strandBuilder[i] == 'A')
                    strandBuilder[i] = 'T';

                else if (strandBuilder[i] == 'T')
                    strandBuilder[i] = 'A';

                else if (strandBuilder[i] == 'G')
                    strandBuilder[i] = 'C';

                else
                    strandBuilder[i] = 'G';
            }
            strand = strandBuilder.ToString();
            return strand;
        }


    }
}




