
$(function () {
    $("body").on("change", "#RankedInterpreterLocationFirst, #RankedInterpreterLocationSecond, #RankedInterpreterLocationThird", function (event) {
        checkInterpreterLocation();
        var $id = $(this)[0].id;
        var $val = $(this).val();
        var $groupBox = $(this).parents(".location-group");
        if ($val === "" || $val === null) {
            $(".address-" + $id).hide();
            $groupBox.removeClass("group-box");
            //Hide any siblings after
            $groupBox.nextAll(".location-group").hide();
        }
        else {
            var $next = $groupBox.next(".location-group");
            $next.show();
            if (hasValue($next.find(".interpreter-group > select").val())) {
                $next.next(".location-group").show();
            }

            $(this).parents(".location-group").addClass("group-box");
            $(".address-" + $id).show();
            var isNewOrder = ($("#UserDefaultSettings_OffSitePhoneContactInformation").val() != null);
            if ($val === "OffSitePhone" || $val === "OffSiteVideo") {
                $(".address-" + $id + " > .address-information").hide();
                $(".address-" + $id + " > .off-site-information").show();
                if (isNewOrder) {
                    var valueToSet = $val === "OffSitePhone" ? $("#UserDefaultSettings_OffSitePhoneContactInformation").val() : $("#UserDefaultSettings_OffSiteVideoContactInformation").val();
                    $("#" + $id + "AddressModel_OffSiteContactInformation").val(valueToSet);
                }
            }
            else {

                $(".address-" + $id + " > .address-information").show();
                $(".address-" + $id + " > .off-site-information").hide();
                if (isNewOrder) {
                    var streetToSet = $val === "OnSite" ? $("#UserDefaultSettings_OnSiteLocationStreet").val() : $("#UserDefaultSettings_OffSiteDesignatedLocationStreet").val();
                    var cityToSet = $val === "OnSite" ? $("#UserDefaultSettings_OnSiteLocationCity").val() : $("#UserDefaultSettings_OffSiteDesignatedLocationCity").val();
                    $("#" + $id + "AddressModel_LocationStreet").val(streetToSet);
                    $("#" + $id + "AddressModel_LocationCity").val(cityToSet);
                }
            }
        }
        var isOnsiteSelected = false;
        $(".location-group select[id^=RankedInterpreterLocation]:visible").each(function () {
            switch ($(this).val()) {
                default:
                    break;
                case "OnSite":
                case "OffSiteDesignatedLocation":
                    isOnsiteSelected = true;
                    break;
            }
        });

        if (isOnsiteSelected) {
            $(".allow-more-travel-cost").show();
        }
        else {
            $(".allow-more-travel-cost").hide();
        }

    });

    $("#RankedInterpreterLocationFirst, #RankedInterpreterLocationSecond, #RankedInterpreterLocationThird").trigger("change");
});

function checkInterpreterLocation() {
    var no1 = $("#RankedInterpreterLocationFirst").val();
    var no2 = $("#RankedInterpreterLocationSecond").val();
    var no3 = $("#RankedInterpreterLocationThird").val();
    var validator = $("#interpreterLocationValidator");
    if (hasValue(no1) && no1 === no2) {
        triggerValidator("Inställelsesätt i första och andra hand kan inte vara samma", validator);
    }
    else if (hasValue(no1) && hasValue(no2) && no1 === no3) {
        triggerValidator("Inställelsesätt i första och tredje hand kan inte vara samma", validator);
    }
    else if (hasValue(no2) && no2 === no3) {
        triggerValidator("Inställelsesätt i andra och tredje hand kan inte vara samma", validator);
    }
    else {
        validator.hide();
        $('#send').attr('disabled', false);
    }
}

function hasValue(selectedValue) {
    return (selectedValue != null && selectedValue.length > 0);
}

function triggerValidator(message, validatorId) {
    $('#send').attr('disabled', true);
    validatorId.empty();
    validatorId.append(message);
    validatorId.show();
}
