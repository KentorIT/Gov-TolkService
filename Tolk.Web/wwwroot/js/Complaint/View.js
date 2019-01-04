$(function () {
    $("body").on("click", ".btn-dispute", function () {
        event.preventDefault();
        triggerValidator("Ange meddelande vid bestridande", $(this), $("#disputeComplaintValidator"), "#DisputeMessage");
    });
    $("body").on("click", ".btn-accept-complaint", function (event) {
        event.preventDefault();
        triggerValidator("Ange meddelande vid svar", $(this), $("#acceptDisputeValidator"), "#AnswerDisputedMessage");
    });
    $("body").on("click", ".btn-refute", function (event) {
        event.preventDefault();
        triggerValidator("Ange meddelande vid svar", $(this), $("#refuteDisputeValidator"), "#AnswerDisputedMessage");
    });
});

function triggerValidator(message, button, validatorId, textArea) {
    var $form = button.parents(".modal-content").find("form");
    validatorId.empty();
    if ($form.valid() && $form.find(textArea).val().length > 0) {
        $form.submit();
    }
    else {
        validatorId.append(message);
    }
}