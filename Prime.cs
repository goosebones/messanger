using System;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Messenger {

/*
 * class for extension methods on object instances
 */
static class ExtensionMethods {
    
    /*
     * determine if a BigInteger is probably prime
     *
     * @param value instance that this method is called on
     * @param witnesses number of witnesses in prime calculation
     * @return true if probably prime, false otherwise
     */
    public static Boolean IsProbablyPrime(this BigInteger value, int witnesses = 10) {
        if (value <= 1) return false;
        if (witnesses <= 0) witnesses = 10;
        BigInteger d = value - 1;
        int s = 0;
        while (d % 2 == 0) {
            d /= 2;
            s += 1;
        }

        Byte[] bytes = new Byte[value.ToByteArray().LongLength];
        BigInteger a;
        for (int i = 0; i < witnesses; i++) {
            do {
                var Gen = new Random();
                Gen.NextBytes(bytes);
                a = new BigInteger(bytes);
            } while (a < 2 || a >= value - 2);
            BigInteger x = BigInteger.ModPow(a, d, value);
            if (x == 1 || x == value - 1) continue;
            for (int r = 1; r < s; r++) {
                x = BigInteger.ModPow(x, 2, value);
                if (x == 1) return false;
                if (x == value - 1) break;
            }
            if (x != value - 1) return false;
        }  
            
        return true;
        }

}

/*
 * class to run prime number generation
 */
class Prime {

	public BigInteger primeNum = 0;

    /*
     * begin the prime number generation
     * generates numbers in parallel
     *
     * @param bits number of bits in numbers to generate
     * @param count number of prime numbers to generate
     */
    public BigInteger generate(int bits) {
	    while (this.primeNum == 0) {
		    // begin the generation!
		    Parallel.For(0, 1000, i => {
			    // create byte array of specified length
			    var byteCount = bits / 8;
			    var bytes = new Byte[byteCount];
			    // generate random number of specified bit length
			    var rng = new RNGCryptoServiceProvider();
			    rng.GetBytes(bytes);
			    var bigInt = new BigInteger(bytes);

			    // when a prime number has been found
			    if (bigInt.IsProbablyPrime()) {
				    this.primeNum = bigInt;
			    }
		    });
	    }

	    return this.primeNum;
    }
}



}