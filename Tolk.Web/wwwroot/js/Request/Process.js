$(function () {
    checkRequirements();

    $('#InterpreterId').change(function () {
        if ($(this).val() === "-1") {
            $('#new-interpreter').collapse('show');
        }
        else {
            $('#new-interpreter').collapse('hide');
        }
    });

    $('#InterpreterLocation').change(function () {
        if ($(this).val() === "OffSite") {
            $('#set-expected-travel-costs').collapse('hide');
        }
        else {
            $('#set-expected-travel-costs').collapse('show');
        }
    });

    $('.checkbox').change(function () {
        checkRequirements();
    });

    function checkRequirements() {
        $('#Accept').attr('disabled', false);

        $("input[id$='CanMeetRequirement']").each(function () {
            var isChecked = $(this).is(':checked');
            var isRequired = $('#' + $(this).attr('id').replace('CanMeetRequirement', 'IsRequired')).attr('value') === 'True';

            if (isRequired && !isChecked) {
                $('#Accept').attr('disabled', true);
            }
        });
    }
});