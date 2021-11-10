--use Tolk[]

update AspnetUsers
set PhoneNumber = ltrim(rtrim(PhoneNumber))
from AspNetUsers
Where len(ltrim(rtrim(PhoneNumber))) < len(PhoneNumber)

update AspnetUsers
set PhoneNumberCellphone = ltrim(rtrim(PhoneNumberCellphone))
from AspNetUsers
Where len(ltrim(rtrim(PhoneNumberCellphone))) < len(PhoneNumberCellphone)

update AspnetUsers
set namefamily = ltrim(rtrim(namefamily))
from AspNetUsers
Where len(ltrim(rtrim(namefamily))) < len(namefamily)

update AspnetUsers
set namefirst = ltrim(rtrim(namefirst))
from AspNetUsers
Where len(ltrim(rtrim(namefirst))) < len(namefirst)
