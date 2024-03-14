$(function () {
    var toggleOrderAgreementDate = function (checked) {
        if (checked) {
            $("#UseOrderAgreementsFromDate").parents("div.form-group").removeClass("d-none");
        } else {
            $("#UseOrderAgreementsFromDate").parents("div.form-group").addClass("d-none");

        }
    }
    var setEarliestDateForOrderResponse = function ()
    {
        // Update OrderResponse date so that its start date isn't before OrderAgreement start date 
        var orderAgreementDate = new Date($("#UseOrderAgreementsFromDate").val());
        $("#UseOrderResponsesFromDate").datepicker("setStartDate", orderAgreementDate);
        var orderResponseDate = new Date($("#UseOrderResponsesFromDate").val());
        if (orderResponseDate < orderAgreementDate) {
            $("#UseOrderResponsesFromDate").datepicker("update", orderAgreementDate)
        }
    }
    var toggleEnableUseOrderResponses = function (checked) {
        var $useOrderResponsesHiddenInput = $("input[type=hidden][name^=CustomerSettings][value=UseOrderResponses]");
        if ($useOrderResponsesHiddenInput.length > 0) {
            var $name = $useOrderResponsesHiddenInput.prop("name").replace("CustomerSettingType", "Value");
            var $checkBox = $("[name='" + $name + "']");

            if (checked) {
                $($checkBox).parents("div.checkbox").removeClass("d-none");
            }
            else {
                $checkBox.prop("checked", false);
                toggleUseOrderResponsesFromDate(false);
                $($checkBox).parents("div.checkbox").addClass("d-none");
            }
        }
    }
    var toggleUseOrderResponsesFromDate = function (checked) {
        if (checked) {
            $("#UseOrderResponsesFromDate").parents("div.form-group").removeClass("d-none");
        } else {
            $("#UseOrderResponsesFromDate").parents("div.form-group").addClass("d-none");
        }
    }
    var handleInitialOrderAgreementSetting = function () {
        var $useOrderAgreementsHiddenInput = $("input[type=hidden][name^=CustomerSettings][value=UseOrderAgreements]");
        if ($useOrderAgreementsHiddenInput.length > 0) {
            var $name = $useOrderAgreementsHiddenInput.prop("name").replace("CustomerSettingType", "Value");
            var $isChecked = $("[name='" + $name + "']").is(":checked");
            toggleOrderAgreementDate($isChecked);
            toggleEnableUseOrderResponses($isChecked);
        }
    }
    var handleInitialOrderResponseSetting = function () {
        var $useOrderResponsesHiddenInput = $("input[type=hidden][name^=CustomerSettings][value=UseOrderResponses]");
        if ($useOrderResponsesHiddenInput.length > 0) {
            var $name = $useOrderResponsesHiddenInput.prop("name").replace("CustomerSettingType", "Value");
            var $isChecked = $("[name='" + $name + "']").is(":checked");
            toggleUseOrderResponsesFromDate($isChecked);
        }
    }
    var init = function () {
        handleInitialOrderAgreementSetting();
        handleInitialOrderResponseSetting(); 
        var orderAgreementDate = new Date($("#UseOrderAgreementsFromDate").val());
        $("#UseOrderResponsesFromDate").datepicker("setStartDate", orderAgreementDate);
    }
    $("body").on("change", "input[name^=CustomerSettings]", function () {
        var $id = $(this).prop("name").replace("Value", "CustomerSettingType");
        //get the type from parent- sibling
        if ($("[name='" + $id + "']").val() === "UseOrderAgreements") {
            toggleOrderAgreementDate($(this).is(":checked"));
            toggleEnableUseOrderResponses($(this).is(":checked"));
        }
        if ($("[name='" + $id + "']").val() === "UseOrderResponses") {
            toggleUseOrderResponsesFromDate($(this).is(":checked"));
        }
    });

    $("#UseOrderAgreementsFromDate").on("change", function () {        
        setEarliestDateForOrderResponse();
    });
    init();
});
