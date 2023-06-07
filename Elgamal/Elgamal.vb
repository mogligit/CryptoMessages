Imports System.Numerics
Imports System.Security.Cryptography
Imports System.Threading
Imports PacketType

Public Structure ElgamalKeyPair
    Dim PublicKey As ElgamalPublicKey
    Dim PrivateKey As BigInteger
End Structure

Public Class ElgamalService
    Private LocalKeyPair As ElgamalKeyPair
    Private securityLevel As Integer    'desired key size by the user

    Public ReadOnly Property PublicKey() As ElgamalPublicKey
        Get
            Return LocalKeyPair.PublicKey
        End Get
    End Property

    Public Event OnKeysGenerated(ByVal PublicKey As ElgamalPublicKey)

    Sub New(ByVal KeySize As Integer)
        securityLevel = KeySize
    End Sub
    Sub New()
        securityLevel = 1024
    End Sub

    Public Function GenerateKeys() As ElgamalPublicKey
        Dim p As BigInteger
        Dim g As BigInteger
        Dim d As BigInteger
        Dim y As BigInteger
        Dim timeoutCT As CancellationTokenSource
        Dim valid As Boolean = False
        Do
            timeoutCT = New CancellationTokenSource

            timeoutCT.CancelAfter(TimeSpan.FromSeconds(30))

            'generating a big random prime
            p = GenerateRandomPrime(securityLevel, securityLevel * 2, timeoutCT.Token)
            'computing the primitive root of p
            g = ComputeGenerator(p, timeoutCT.Token)
            'generating the private key
            d = GenerateRandomInteger(1, SizeOf(p))
            'generating public key
            y = BigInteger.ModPow(g, d, p)

            If Not timeoutCT.IsCancellationRequested Then
                LocalKeyPair.PublicKey = New ElgamalPublicKey(p, g, y)
                LocalKeyPair.PrivateKey = d
                valid = True
            End If

        Loop Until valid

        Return LocalKeyPair.PublicKey
        RaiseEvent OnKeysGenerated(LocalKeyPair.PublicKey)
    End Function

    'new key pair for server use
    Public Shared Function GenerateNewKeyPair(ByVal p As BigInteger, ByVal g As BigInteger) As ElgamalKeyPair
        Dim d As BigInteger = GenerateRandomInteger(1, SizeOf(p))
        Dim y As BigInteger = BigInteger.ModPow(g, d, p)

        Dim publicKey As New ElgamalPublicKey(p, g, y)

        Dim keyPair As New ElgamalKeyPair
        keyPair.PrivateKey = d
        keyPair.PublicKey = publicKey

        Return keyPair
    End Function


    Public Shared Function Encrypt(ByVal Plaintext As Byte(), ByVal RemotePublicKey As ElgamalPublicKey) As ElgamalCiphertext
        Debug.WriteLine("--Encryption--")
        Dim i As BigInteger     'single-use random number
        Dim maskingKey As BigInteger 'masking key
        Dim plaintextBytes As List(Of Byte) = Plaintext.ToList

        Dim ciphertext As ElgamalCiphertext
        Dim C1 As BigInteger    'ephimeral key (based on i)
        Dim C2 As New List(Of BigInteger)

        Dim plaintextChunks As New List(Of Byte())    'list of byte arrays
        Dim plaintextIntChunks As New List(Of BigInteger)

        'If the plaintext is bigger than p (the prime in the public key) it will not be correctly encrypted
        'and the data will be useless. So this code here splits the byte array into chunks that are smaller
        'than p and encrypts them separately. An extra byte 255 is added at the end to make sure all bytes
        'are included in the final integer (if the final bytes were all 0s, they would not be included and
        'data would be lost).
        Dim chunkSize As Integer = CInt(Math.Ceiling(SizeOf(RemotePublicKey.p) / 8)) - 3  'size of p in bytes (-1 for extra byte)
        Debug.WriteLine("Size of P in bytes: " & chunkSize.ToString)
        'splitting PlaintextByte into chunks
        Debug.WriteLine("Plaintext: " & BitConverter.ToString(plaintextBytes.ToArray()))
        Do
            Dim currentSection As List(Of Byte) = plaintextBytes.Take(chunkSize).ToList 'splitting sizeOfp amount of bytes
            currentSection.Add(CByte(1)) 'adding an extra byte at the end

            plaintextChunks.Add(currentSection.ToArray)   'adding to the list
            Debug.WriteLine("Added new chunk to list: " & BitConverter.ToString(currentSection.ToArray()))

            If plaintextBytes.Count >= chunkSize Then  'if list is still bigger than p
                plaintextBytes.RemoveRange(0, chunkSize)
            Else
                plaintextBytes.Clear()
            End If
        Loop While plaintextBytes.Count > 0

        'turning all byte() into a multiplicable integers
        For Each plaintextChunk In plaintextChunks
            Dim plainInt As BigInteger = BigInteger.Abs(New BigInteger(plaintextChunk))
            plaintextIntChunks.Add(plainInt)

            Debug.WriteLine("Integers: " & plainInt.ToString)
        Next

        'computing C1 (ephimeral key Ke)
        'C1 = (g^i) mod p
        i = GenerateRandomInteger(2, SizeOf(RemotePublicKey.p - 2))
        C1 = BigInteger.ModPow(RemotePublicKey.g, i, RemotePublicKey.p)
        Debug.WriteLine("C1: " & C1.ToString)

        'computing C2 (the ciphertext). Plaintext = P
        'Km = (y^i) mod p
        'C2 = (P*Km) mod p
        maskingKey = BigInteger.ModPow(RemotePublicKey.y, i, RemotePublicKey.p)
        Debug.WriteLine("Masking key: " & maskingKey.ToString)

        For Each plaintextInt In plaintextIntChunks
            Dim cipherInt As BigInteger = (plaintextInt * maskingKey) Mod RemotePublicKey.p
            C2.Add(cipherInt)

            Debug.WriteLine("Cipher integers: " & cipherInt.ToString)
        Next

        ciphertext = New ElgamalCiphertext(C1, C2.ToArray)

        Return ciphertext
    End Function

    'decrypt with private key stored in the instance of this object
    Public Overloads Function Decrypt(ByVal Ciphertext As ElgamalCiphertext) As Byte()
        Dim plaintextBytes As New List(Of Byte)
        Dim maskingKey As BigInteger
        Dim invModOfKm As BigInteger

        'Recovering masking key Km from the ephimeral key and the private key.
        'Km = Masking key   -   Ke = Ephimeral key
        'Km = (Ke^d) mod p
        maskingKey = BigInteger.ModPow(Ciphertext.C1, LocalKeyPair.PrivateKey, LocalKeyPair.PublicKey.p)
        Debug.WriteLine("Masking key: " & maskingKey.ToString)

        'Calculating the inverse modulo of Km
        '(Km^-1) mod p
        invModOfKm = InverseMod(maskingKey, LocalKeyPair.PublicKey.p)

        'Computing plaintext
        'P = (C2*(Km^-1)) mod p
        For Each cipherInt In Ciphertext.C2
            Debug.WriteLine("Cipher integer: " & cipherInt.ToString)

            Dim plaintextInt As BigInteger
            'decrypting the integer into a byte array
            plaintextInt = (cipherInt * invModOfKm) Mod LocalKeyPair.PublicKey.p
            Debug.WriteLine("Plain integer: " & plaintextInt.ToString)

            'because the byte array has an extra byte it needs to be removed
            Dim plaintextChunk As List(Of Byte) = plaintextInt.ToByteArray.ToList
            plaintextChunk.RemoveAt(plaintextChunk.Count - 1) 'removing last bit

            'concatenating all byte lists
            plaintextBytes = plaintextBytes.Concat(plaintextChunk).ToList
        Next

        Return plaintextBytes.ToArray
    End Function
    'decrypt with a remote private key
    Public Overloads Shared Function Decrypt(ByVal Ciphertext As ElgamalCiphertext, ByVal RemoteKeyPair As ElgamalKeyPair) As Byte()
        Dim plaintextList As New List(Of Byte)
        Dim maskingKey As BigInteger
        Dim invModOfKm As BigInteger

        'Recovering masking key Km from the ephimeral key and the private key.
        'Km = Masking key   -   Ke = Ephimeral key
        'Km = (Ke^d) mod p
        maskingKey = BigInteger.ModPow(Ciphertext.C1, RemoteKeyPair.PrivateKey, RemoteKeyPair.PublicKey.p)

        'Calculating the inverse modulo of Km
        '(Km^-1) mod p
        invModOfKm = InverseMod(maskingKey, RemoteKeyPair.PublicKey.p)

        'Computing plaintext
        'P = (C2*(Km^-1)) mod p
        For Each cipherInt In Ciphertext.C2
            Dim plaintextInt As BigInteger
            'decrypting the integer into a byte array
            plaintextInt = (cipherInt * invModOfKm) Mod RemoteKeyPair.PublicKey.p

            'because the byte array has an extra byte it needs to be removed
            Dim plaintextIntByteArray As List(Of Byte) = plaintextInt.ToByteArray.ToList
            plaintextIntByteArray.RemoveAt(plaintextIntByteArray.Count - 1) 'removing last bit

            'concatenating all byte lists
            plaintextList = plaintextList.Concat(plaintextIntByteArray).ToList
        Next

        'from number to byte()
        Return plaintextList.ToArray
    End Function

    Private Shared Function InverseMod(ByVal a As BigInteger, ByVal m As BigInteger) As BigInteger
        Return BigInteger.ModPow(a, m - 2, m)
    End Function

    Public Shared Function SizeOf(ByVal n As BigInteger) As Integer
        Dim log As Double = BigInteger.Log(n + 1, 2)
        Return CInt(Math.Ceiling(log))
    End Function
    Public Shared Function GenerateRandomInteger(ByVal lowerBoundBits As Double, ByVal upperBoundBits As Double) As BigInteger
        Dim randomSize As Integer
        Dim randomArray As Byte()
        Dim randomInteger As BigInteger
        Dim randomSizeGenerator As New Random

        Do
            randomSize = randomSizeGenerator.Next(CInt(lowerBoundBits / 8), CInt(upperBoundBits / 8))
            ReDim randomArray(randomSize - 1)
            RandomNumberGenerator.Create.GetBytes(randomArray)
            randomInteger = BigInteger.Abs(New BigInteger(randomArray))
        Loop While randomInteger = 0

        Return randomInteger
    End Function

    Private Function GenerateRandomPrime(ByVal lowerBoundBits As Integer, ByVal upperBoundBits As Integer, ByVal timeoutCT As CancellationToken) As BigInteger  'bounds in BITS
        Dim randomInteger As BigInteger
        Dim completionSource As New TaskCompletionSource(Of BigInteger)

        timeoutCT.Register(Sub() completionSource.TrySetCanceled())
        Do
            randomInteger = GenerateRandomInteger(lowerBoundBits, upperBoundBits)
            If randomInteger Mod 2 <> 0 Then
                If IsPrime(randomInteger) Then
                    Return randomInteger
                End If
            End If
        Loop While Not timeoutCT.IsCancellationRequested

        Return Nothing
    End Function
    Private Function IsPrime(ByVal p As BigInteger) As Boolean
        Return (p = 2) Or (FermatTest(p) AndAlso MillerRabinTest(p))
    End Function
    Private Function FermatTest(ByVal p As BigInteger) As Boolean
        'Fermat's Primalty test states that for any prime integer p,
        'a^(p-1) mod p = 1
        'where a is any random integer beween 1 and p-1

        Dim a As Integer
        a = 2

        Return (BigInteger.ModPow(a, p - 1, p) = 1)
    End Function
    Private Function MillerRabinTest(ByVal n As BigInteger) As Boolean
        If n Mod 2 = 0 Then
            Return False
        End If

        Dim nMinusOne As BigInteger = n - 1     'n - 1 = 2^s * m
        Dim m As BigInteger
        Dim s As Double

        m = nMinusOne
        While m Mod 2 = 0       'finding m
            m = m / 2           'divide by 2 until result is odd
        End While

        Dim powerOfTwo As BigInteger
        powerOfTwo = nMinusOne / m
        s = CDbl(BigInteger.Log(powerOfTwo, 2))     'finding s

        Dim a As BigInteger = 2
        Dim i As Integer = 0        'iteration counter

        Do
            Dim iResult As BigInteger

            iResult = BigInteger.ModPow(a, (BigInteger.Pow(2, i)) * m, n)
            'iteration result = a^((2^i)*m) mod n

            If (iResult = 1 And i = 0) Or iResult = nMinusOne Then
                Return True
            ElseIf (iResult = 1 And Not i = 0) Or i = s - 1 Then
                Return False
            Else
                i += 1
            End If
        Loop
    End Function

    Private Function ComputeGenerator(ByVal p As BigInteger, ByVal timeoutCT As CancellationToken) As BigInteger
        Dim n As BigInteger = p - 1
        Dim factorsOfN As BigInteger() = PrimeFactorisation(n, timeoutCT)
        Dim numberToTest As BigInteger = 0
        Dim valid As Boolean

        Do
            valid = True
            numberToTest = GenerateRandomInteger(1, SizeOf(p))

            If numberToTest < p Then
                For Each factor In factorsOfN
                    'testing random numbers with all factors to see if they are a generator.
                    'if any of them meets this condition, they are not.
                    'if numToTest^((p-1)/factor) mod p = 1, it is not a generator
                    If BigInteger.ModPow(numberToTest, BigInteger.Divide(n, factor), p) = 1 Then
                        valid = False
                    End If
                Next
            End If
        Loop Until valid Or timeoutCT.IsCancellationRequested

        If timeoutCT.IsCancellationRequested Then
            Return Nothing
        Else
            Return numberToTest
        End If
    End Function
    Private Function IsPerfectSquare(ByVal n As BigInteger, Optional ByRef SquareRoot As BigInteger = Nothing) As Boolean
        SquareRoot = NewtonSqrt(n, n / 2)
        If BigInteger.Pow(SquareRoot, 2) = n Then
            Return True
        Else
            Return False
        End If
    End Function
    Private Function NewtonSqrt(ByVal n As BigInteger, ByVal a As BigInteger) As BigInteger
        'Newton-Rhapson method
        Dim result As BigInteger
        result = a - ((BigInteger.Pow(a, 2) - n) / BigInteger.Multiply(2, a))

        If result = a Then
            Return result - 1       'subtract 1 because BigInteger rounds up
        Else
            Return (NewtonSqrt(n, result))
        End If
    End Function
    Private Function PrimeFactorisation(ByVal n As BigInteger, ByVal CT As CancellationToken) As BigInteger()
        Dim primeFactors As New List(Of BigInteger)
        Dim numbersToFactor As New Stack(Of BigInteger)

        numbersToFactor.Push(n)

        Do While numbersToFactor.Count > 0 And Not CT.IsCancellationRequested
            Dim currentInt As BigInteger = numbersToFactor.Pop
            If IsPrime(currentInt) Then
                primeFactors.Add(currentInt)
            Else
                Dim factor As BigInteger = PollardRho(currentInt, CT)
                numbersToFactor.Push(factor)                '2 factors get pushed
                numbersToFactor.Push(currentInt / factor)
            End If
        Loop

        Return ReduceList(primeFactors).ToArray
    End Function
    Private Function ReduceList(ByVal list As List(Of BigInteger)) As List(Of BigInteger)
        'removes all duplicates from list
        Dim finalItems As New List(Of BigInteger)
        For Each item In list
            If Not finalItems.Contains(item) Then
                finalItems.Add(item)
            End If
        Next
        Return finalItems
    End Function
    Private Function PollardRho(ByVal n As BigInteger, ByVal CT As CancellationToken) As BigInteger
        Dim result As BigInteger
        Dim root As New BigInteger

        If n Mod 2 = 0 Then
            Return 2
        ElseIf IsPerfectSquare(n, root) Then    'root is passed as ByVal so it can be written
            Return root
        Else
            Dim x As BigInteger
            Dim y As BigInteger

            Dim c As BigInteger = BigInteger.MinusOne
            Dim seed As BigInteger = 2
            Dim g = Function(input As BigInteger) ((input * input) + c) Mod n
            Do
                x = seed
                y = x
                result = 1
                While result = 1 And Not CT.IsCancellationRequested              'while a divisor is not found
                    x = g(x)
                    y = g(g(y))
                    result = BigInteger.GreatestCommonDivisor(BigInteger.Abs(x - y), n)
                End While

                If result = n Or result = 0 Or result = 1 Then
                    c += 1
                    seed += 1
                Else
                    Return result
                End If
            Loop Until CT.IsCancellationRequested
            Return Nothing
        End If
    End Function
End Class