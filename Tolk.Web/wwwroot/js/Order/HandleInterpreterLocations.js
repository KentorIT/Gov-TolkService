
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
            var $select = $next.find(".interpreter-group > select");
            // Make sure that the following cannot select the selected value, and if this is the selected value, unmake the selection in the siling.
            if ($select.val() === $val) {
                $select.val("").trigger("change");
            }
            //$select.find($("option[value=" + $val + "]")).attr('disabled', 'disabled');
            if ($next.find(".interpreter-group > select").val() !== "") {
                $next.next(".location-group").show();
            }

            $(this).parents(".location-group").addClass("group-box");
            $(".address-" + $id).show();
            if ($val === "OffSitePhone" || $val === "OffSiteVideo") {
                $(".address-" + $id + " > .address-information").hide();
                $(".address-" + $id + " > .off-site-information").show();
            } else {
                $(".address-" + $id + " > .address-information").show();
                $(".address-" + $id + " > .off-site-information").hide();
            }
        }
    });
    $("#RankedInterpreterLocationFirst, #RankedInterpreterLocationSecond, #RankedInterpreterLocationThird").trigger("change");
});

function checkInterpreterLocation() {
    var no1 = $("#RankedInterpreterLocationFirst").val();
    var no2 = $("#RankedInterpreterLocationSecond").val();
    var no3 = $("#RankedInterpreterLocationThird").val();
    var validator = $("#interpreterLocationValidator");
    if (no1 != "" && no1 == no2) {
        triggerValidator("Inställelsesätt i första och andra hand kan inte vara samma", validator);
    }
    else if (no1 != "" && no1 == no3) {
        triggerValidator("Inställelsesätt i första och tredje hand kan inte vara samma", validator);
    }
    else if (no2 != "" && no2 == no3) {
        triggerValidator("Inställelsesätt i andra och tredje hand kan inte vara samma", validator);
    }
    else {
        validator.hide();
        $('#send').attr('disabled', false);
    }
}

function triggerValidator(message, validatorId) {
    $('#send').attr('disabled', true);
    validatorId.empty();
    validatorId.append(message);
    validatorId.show();
}
