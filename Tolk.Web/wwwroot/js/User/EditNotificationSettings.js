
$(function () {
    var toggleTextBoxVisibility = function (visible, box) {
        if (visible) {
            box.show();
        } else {
            box.hide();
        }
    };

    $(".selection-box input[type=checkbox]").each(function () {
        toggleTextBoxVisibility($(this).is(":checked"), $(this).closest(".selection-box").siblings(".selection-textbox"));
    });
    $("body").on("change", ".selection-box input[type=checkbox]", function () {
        toggleTextBoxVisibility($(this).is(":checked"), $(this).closest(".selection-box").siblings(".selection-textbox"));
    });
});
