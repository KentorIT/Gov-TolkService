$(function () {
    var toggleLocalAdminVisibility = function (show, localAdminBox, white) {
        if (show) {
            localAdminBox.show();
            white.hide();
        } else {
            localAdminBox.hide();
            white.show();
        }
    };

    $(".select-connect input[type=checkbox]").each(function () {
        toggleLocalAdminVisibility($(this).is(":checked"), $(this).closest(".select-connect").siblings(".select-localadmin"), $(this).closest(".select-connect").siblings(".select-localadmin").siblings(".non-unit-local-admin"));
    });

    $("body").on("change", ".select-connect input[type=checkbox]", function () {
        toggleLocalAdminVisibility($(this).is(":checked"), $(this).closest(".select-connect").siblings(".select-localadmin"), $(this).closest(".select-connect").siblings(".select-localadmin").siblings(".non-unit-local-admin"));
    });

    $(document).ready(function () {
        toggleRoles($("#OrganisationIdentifier option:selected"));
    });

    $("body").on("change", "#OrganisationIdentifier", function () {
        toggleRoles($("#OrganisationIdentifier option:selected"));
    });

    function toggleRoles(selectedItem) {
        if (selectedItem.attr('value') !== undefined) {
            if (selectedItem.data('additional') === "GovernmentBody") {
                $('.CentralOrderHandlerCheckBox').show();
                $('.OrganisationAdministratorCheckBox').show();
                $('.ApplicationAdministratorCheckBox').hide();
                $('.SystemAdministratorCheckBox').hide();
                $('.ImpersonatorCheckBox').hide();
            }
            else if (selectedItem.data('additional') === "Broker") {
                $('.CentralOrderHandlerCheckBox').hide();
                $('.ApplicationAdministratorCheckBox').hide();
                $('.SystemAdministratorCheckBox').hide();
                $('.ImpersonatorCheckBox').hide();
                $('.OrganisationAdministratorCheckBox').show();
            }
            else if (selectedItem.data('additional') === "Owner") {
                $('.CentralOrderHandlerCheckBox').hide();
                $('.OrganisationAdministratorCheckBox').hide();
                $('.ApplicationAdministratorCheckBox').show();
                $('.SystemAdministratorCheckBox').show();
                $('.ImpersonatorCheckBox').show();
            }
            else {
                $('.CentralOrderHandlerCheckBox').hide();
                $('.OrganisationAdministratorCheckBox').hide();
                $('.ApplicationAdministratorCheckBox').hide();
                $('.SystemAdministratorCheckBox').hide();
                $('.ImpersonatorCheckBox').hide();
            }
        }
    };

});