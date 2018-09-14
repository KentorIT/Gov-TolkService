
$(function () {
    $("body").on("change", "#RankedInterpreterLocationFirst, #RankedInterpreterLocationSecond, #RankedInterpreterLocationThird", function (event) {
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
            if ($val === "OffSite") {
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
