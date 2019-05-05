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
});