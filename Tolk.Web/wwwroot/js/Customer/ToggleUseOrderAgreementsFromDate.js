$(function () {
    var toggleOrderAgreementDate = function (checked) {
        if (checked) {
            $("#UseOrderAgreementsFromDate").parents("div.form-group").removeClass("d-none");
        } else {
            $("#UseOrderAgreementsFromDate").parents("div.form-group").addClass("d-none");

        }
    }
    var init = function() {
        var $hidden = $("input[type=hidden][name^=CustomerSettings][value=UseOrderAgreements]");
        if ($hidden.length > 0) {
            var $name = $hidden.prop("name").replace("CustomerSettingType", "Value");
            var $isChecked = $("[name='" + $name + "']").is(":checked");
            toggleOrderAgreementDate($isChecked);
        }
    }
    $("body").on("change", "input[name^=CustomerSettings]", function () {
        var $id = $(this).prop("name").replace("Value", "CustomerSettingType");
        //get the type from parent- sibling
        if ($("[name='" + $id + "']").val() === "UseOrderAgreements") {
            toggleOrderAgreementDate($(this).is(":checked"));
        }
    });
    init();
});
