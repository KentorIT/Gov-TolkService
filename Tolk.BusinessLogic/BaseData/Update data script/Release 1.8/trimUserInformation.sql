--use Tolk[]

update AspnetUsers
set PhoneNumber = trim(PhoneNumber)
from AspNetUsers
Where len(trim(PhoneNumber)) < len(PhoneNumber)

update AspnetUsers
set PhoneNumberCellphone = trim(PhoneNumberCellphone)
from AspNetUsers
Where len(trim(PhoneNumberCellphone)) < len(PhoneNumberCellphone)

update AspnetUsers
set namefamily = trim(namefamily)
from AspNetUsers
Where len(trim(namefamily)) < len(namefamily)

update AspnetUsers
set namefirst = trim(namefirst)
from AspNetUsers
Where len(trim(namefirst)) < len(namefirst)