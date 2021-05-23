using EStore_API.Helper;
using EStore_API.Interfaces;
using EStore_API.Models;
using EStore_API.Models.ViewModel.Constant;
using EStore_API.Models.ViewModel.DTO;
using EStore_API.Models.ViewModel.Enum;
using EStore_API.Models.ViewModel.RequestModels;
using EStore_API.Models.ViewModel.ResponseModels;
using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EStore_API.Manager
{
    public class EStoreManager
    {
        EVoucherSystemDBContext _dbContext;
        private readonly IConfiguration _configuration;
        IRepository<EvoucherTb> _evoucherepo;
        IRepository<PaymentMethodTb> _paymentmethodrepo;
        IRepository<GeneratedEvoucherTb> _generatedEvoucherrepo;
        IRepository<PurchaseHistoryTb> _purchaseHistoryrepo;
        IRepository<PurchaseOrderTb> _purchaseOrderrepo;

        public EStoreManager(IConfiguration configuration)
        {
            _dbContext = new EVoucherSystemDBContext();
            _configuration = configuration;
            _evoucherepo = new BaseRepository<EvoucherTb>(_dbContext);
            _paymentmethodrepo = new BaseRepository<PaymentMethodTb>(_dbContext);
            _generatedEvoucherrepo = new BaseRepository<GeneratedEvoucherTb>(_dbContext);
            _purchaseHistoryrepo = new BaseRepository<PurchaseHistoryTb>(_dbContext);
            _purchaseOrderrepo = new BaseRepository<PurchaseOrderTb>(_dbContext);
        }

        public EstoreAccessTokenResponse GetAccessToken(EstoreAccessTokenRequest _request)
        {
            EstoreAccessTokenResponse response = new EstoreAccessTokenResponse();

            if (_configuration["EStoreClientID"] == _request.ClientID)
            {
                GetGenerateTokenDTO generateTokenDto = new GetGenerateTokenDTO
                {
                    Audience = _configuration["Audience"],
                    Issuer = _configuration["Issuer"],
                    PrivateKey = _configuration["RsaPrivateKey"],
                    TokenExpiryMinute = Int32.Parse(_configuration["TokenExpiryMinute"]),
                    RefreshTokenExpiryMinute = Int32.Parse(_configuration["RefreshTokenExpiryMinute"]),
                    UserId = 0,
                    UserName = "EstoreClient"
                };
                TokenGeneratedDTO generatedToken = JwtHandler.GenerateToken(generateTokenDto);
                if (generatedToken != null && string.IsNullOrEmpty(generatedToken.ErrorStatus))
                {
                    response.AccessToken = generatedToken.AccessToken;
                    response.AccessTokenExpireMinutes = generatedToken.TokenExpiresMinute;
                    response.RefreshToken = generatedToken.RefreshToken;
                    response.RefreshTokenExpireMinutes = generatedToken.TokenExpiresMinute;
                }
                else
                {
                    response.StatusCode = 401;
                    response.ErrorType = "Unauthorized Request";
                    response.ErrorMessage = "Unable to generate Token.";
                }
            }
            else
            {
                response.StatusCode = 401;
                response.ErrorType = "Unauthorized Request";
                response.ErrorMessage = "Invalid Client ID.";
            }
            return response;
        }

        public PagedList<GetEVoucherListingResponse> GetStoreEvoucherList(GetEVoucherListingRequest _request)
        {
            var evoucherList = (from e in _evoucherepo.Get
                                where (_request.Status == null || e.Status == _request.Status)
                                && e.Status == (int)RecordStatus.Active && e.Quantity > 0
                                select new GetEVoucherListingResponse
                                {
                                    VoucherNo = e.VoucherNo,
                                    Title = e.Title,
                                    ExpiryDate = e.ExpiryDate,
                                    Quantity = e.Quantity,
                                    Status = e.Status,
                                    SellingPrice = e.SellingPrice,
                                    VoucherAmount = e.VoucherAmount
                                }).AsQueryable();
            return PagedList<GetEVoucherListingResponse>.ToPagedList(evoucherList,
         _request.PageNumber,
         _request.PageSize);
        }

        public GetEVoucherDetailResponse GetEvoucherDetail(GetEVoucherDetailRequest _request)
        {
            GetEVoucherDetailResponse response = new GetEVoucherDetailResponse();
            response = (from e in _evoucherepo.Get
                        where e.VoucherNo == _request.VoucherNo
                        select new GetEVoucherDetailResponse
                        {
                            VoucherNo = e.VoucherNo,
                            Title = e.Title,
                            BuyType = e.BuyType,
                            Description = e.Description,
                            ExpiryDate = e.ExpiryDate,
                            GiftPerUserLimit = e.GiftPerUserLimit,
                            Image = Path.Combine(_configuration["BaseURL"], e.ImagePath),
                            MaxLimit = e.MaxLimit,
                            PaymentMethod = e.PaymentMethod,
                            Quantity = e.Quantity,
                            SellingDiscount = e.SellingDiscount,
                            SellingPrice = e.SellingPrice,
                            Status = e.Status,
                            VoucherAmount = e.VoucherAmount,
                        }).FirstOrDefault();

            return response;
        }

        public List<GetPaymentMethodListResponse> GetPaymentMethodList()
        {
            List<GetPaymentMethodListResponse> response = new List<GetPaymentMethodListResponse>();
            response = (from p in _paymentmethodrepo.Get
                        where p.Status == (int)RecordStatus.Active
                        select new GetPaymentMethodListResponse
                        {
                            PaymentMethod = p.PaymentMethod,
                            Description = p.Description,
                            DiscountPercentage = p.DiscountPercentage,
                            HasDiscount = p.IsDiscount,
                            Status = p.Status
                        }).ToList();
            return response;
        }

        public CheckStockAvaliableResponse CheckStockAvaliable(CheckStockAvaliableRequest _request)
        {
            var response = (from p in _evoucherepo.Get
                            where p.Status == (int)RecordStatus.Active
                            && p.Quantity > 0
                            && p.ExpiryDate > DateTime.Now
                            && p.VoucherNo == _request.VoucherNo
                            select new CheckStockAvaliableResponse
                            {
                                isAvaliable = true,
                                RemainingQuantity = p.Quantity
                            }
                        ).FirstOrDefault();

            if (response == null)
            {
                response = new CheckStockAvaliableResponse
                {
                    isAvaliable = false,
                    RemainingQuantity = 0
                };
            }
            return response;
        }

        public CheckPromoCodeResponse CheckPromoCode(CheckPromoCodeRequest _request)
        {
            var response = (from ge in _generatedEvoucherrepo.Get
                            where ge.PromoCode == _request.PromoCode
                            && ge.ExpiryDate > DateTime.Now
                            && ge.OwnerPhone == _request.Phone
                            select new CheckPromoCodeResponse
                            {
                                Status = (PromoCodeStatus)ge.Status,
                                PromoAmount = ge.VoncherAmount
                            }
                        ).FirstOrDefault();
            if (response == null)
            {
                response = new CheckPromoCodeResponse
                {
                    Status = PromoCodeStatus.InValid,
                    PromoAmount = 0
                };
            }

            return response;
        }

        public PagedList<GetPurchaseHistoryResponse> GetPurchaseHistory(GetPurchaseHistoryRequest _request)
        {
            GetPurchaseHistoryResponse response = new GetPurchaseHistoryResponse();
            var eVoucherList = (from p in _purchaseHistoryrepo.Get
                                join ge in _generatedEvoucherrepo.Get
                                on p.PromoCode equals ge.PromoCode
                                where ge.Status >= (int)PromoCodeStatus.Used
                                && p.Status == (int)RecordStatus.Active
                                && _request.PurchaseFromDate == null ? true : p.PurchaseDate >= _request.PurchaseFromDate
                                && _request.PurchaseToDate == null ? true : p.PurchaseDate <= _request.PurchaseToDate
                                select new GetPurchaseHistoryResponse
                                {
                                    PromoCode = p.PromoCode,
                                    QR_Image_Path = ge.Status != (int)PromoCodeStatus.Used ? ge.QrimagePath : "",
                                    IsUsed = ge.Status == (int)PromoCodeStatus.Used,
                                    PurchaseHistoryId = p.PurchaseHistoryId,
                                }
                                ).AsQueryable();
            return PagedList<GetPurchaseHistoryResponse>.ToPagedList(eVoucherList,
        _request.PageNumber,
        _request.PageSize);
        }

        public BuyEVoucherResponse BuyEVoucher(BuyEVoucherRequest _request)
        {
            BuyEVoucherResponse response = new BuyEVoucherResponse();
            string validateMsg = "";
            validateMsg = ValidateBuyEVoucher(_request);
            if (string.IsNullOrEmpty(validateMsg))
            {
                using (var dbContextTransaction = _dbContext.Database.BeginTransaction())
                {
                    try
                    {
                        var tblEvoucher = _evoucherepo.Get.Where(e => e.VoucherNo == _request.VoucherNo).FirstOrDefault();
                        if (tblEvoucher == null)
                        {
                            validateMsg = "No Voucher Available.";
                        }
                        else
                        {
                            if (tblEvoucher.ExpiryDate < DateTime.Now && tblEvoucher.Status != (int)RecordStatus.Active)
                            {
                                validateMsg = $"{validateMsg}\r\nVoucher has been expired or out of stock.";
                            }
                            else if (tblEvoucher.Quantity <= 0)
                            {
                                validateMsg = $"{validateMsg}\r\nOut of stock.";
                            }
                            else if (tblEvoucher.Quantity < _request.Quantity)
                            {
                                validateMsg = $"{validateMsg}\r\nOrder quantity exceed the avaliable stock.";
                            }
                            else
                            {
                                var previousOrderList = (from p in _purchaseOrderrepo.Get
                                                         where p.VoucherNo == _request.VoucherNo
                                                         && p.BuyerPhone == _request.BuyerPhone
                                                         select new
                                                         {
                                                             p.BuyType,
                                                             p.Quantity
                                                         }
                                                       ).ToList();

                                if (previousOrderList == null || previousOrderList.Count <= 0)
                                {
                                    if (_request.BuyType == Constants.EVOUCHER_BUY_TYPE_ONLYME
                                        && _request.Quantity > tblEvoucher.MaxLimit
                                        )
                                    {
                                        validateMsg = $"{validateMsg}\r\nReach Limitted Quantity,You can't buy anymore.";
                                    }
                                    else if (_request.Quantity > tblEvoucher.GiftPerUserLimit)
                                    {
                                        validateMsg = $"{validateMsg}\r\nReach Limitted Gift Quantity,You can't buy anymore.";
                                    }
                                }
                                else
                                {
                                    var buyGroup = previousOrderList.GroupBy(x => x.BuyType)
                                                            .Select(x => new
                                                            {
                                                                BuyType = x.First().BuyType,
                                                                Quantity = x.Sum(x => x.Quantity)
                                                            }).ToList();
                                    var OwnUsageQuantity = buyGroup.Where(x => x.BuyType == Constants.EVOUCHER_BUY_TYPE_ONLYME).Select(x => x.Quantity).FirstOrDefault();
                                    var GiftUsageQuantity = buyGroup.Where(x => x.BuyType == Constants.EVOUCHER_BUY_TYPE_GIFT).Select(x => x.Quantity).FirstOrDefault();
                                    var totalUsage = OwnUsageQuantity + GiftUsageQuantity;

                                    if (_request.Quantity + totalUsage > tblEvoucher.MaxLimit)
                                    {
                                        if (totalUsage > tblEvoucher.MaxLimit)
                                            validateMsg = $"{validateMsg}\r\nReach Limitted Quantity,You can buy anymore.";
                                        else
                                            validateMsg = $"{validateMsg}\r\nReach Limitted Quantity,You can buy only {tblEvoucher.MaxLimit - totalUsage} voucher.";

                                    }
                                    else if (_request.BuyType == Constants.EVOUCHER_BUY_TYPE_ONLYME
                                       && _request.Quantity + OwnUsageQuantity > tblEvoucher.MaxLimit
                                       )
                                    {
                                        if (OwnUsageQuantity > tblEvoucher.MaxLimit)
                                            validateMsg = $"{validateMsg}\r\nOwn Usage Reach Limitted Quantity,You can't buy anymore.";
                                        else
                                            validateMsg = $"{validateMsg}\r\nOwn Usage Reach Limitted Quantity,You can buy only {tblEvoucher.MaxLimit - OwnUsageQuantity} voucher.";
                                    }
                                    else if (_request.Quantity + GiftUsageQuantity > tblEvoucher.GiftPerUserLimit)
                                    {
                                        if (GiftUsageQuantity > tblEvoucher.GiftPerUserLimit)
                                            validateMsg = $"{validateMsg}\r\nGift Usage Reach Limitted Quantity,You can't buy anymore.";
                                        else
                                            validateMsg = $"{validateMsg}\r\nGift Usage Reach Limitted Quantity,You can buy only {tblEvoucher.MaxLimit - GiftUsageQuantity} voucher.";
                                    }

                                }

                                if (validateMsg == "")
                                {
                                    var UpdatetblEvoucher = _evoucherepo.Get.Where(e => e.VoucherNo == _request.VoucherNo).FirstOrDefault();
                                    UpdatetblEvoucher.Quantity = UpdatetblEvoucher.Quantity - _request.Quantity;
                                    decimal totalPrice = UpdatetblEvoucher.SellingPrice;
                                    short sellingDiscount;
                                    if (_request.PaymentMethod == UpdatetblEvoucher.PaymentMethod
                                       && UpdatetblEvoucher.SellingDiscount != null)
                                    {
                                        var discountAmount = totalPrice * (decimal)((UpdatetblEvoucher.SellingDiscount ?? 0) / 100.0);
                                        totalPrice = totalPrice - discountAmount;
                                        if (totalPrice < 0)
                                            totalPrice = 0;
                                        sellingDiscount = UpdatetblEvoucher.SellingDiscount ?? 0;
                                    }
                                    else
                                    {
                                        sellingDiscount = 0;
                                    }

                                    var pOrderList = (from v in _purchaseOrderrepo.Get
                                                      select new
                                                      {
                                                          v.Id
                                                      }
                                     ).ToList();

                                    int maxNo = 1;
                                    if (pOrderList != null && pOrderList.Count > 0)
                                    {
                                        maxNo = pOrderList.Max(x => x.Id);
                                        maxNo++;
                                    }

                                    PurchaseOrderTb order = new PurchaseOrderTb
                                    {
                                        PurchaseOrderNo = "PO-" + maxNo.ToString().PadLeft(6, '0'),
                                        BuyerName = _request.BuyerName,
                                        BuyerPhone = _request.BuyerPhone,
                                        BuyType = _request.BuyType,
                                        OrderDate = DateTime.Now,
                                        PaymentMethod = _request.PaymentMethod,
                                        SellingDiscount = sellingDiscount,
                                        Quantity = _request.Quantity,
                                        Status = (int)RecordStatus.Active,
                                        TotalSellingAmount = totalPrice,
                                        SellingPrice = UpdatetblEvoucher.SellingPrice,
                                        ExpiryDate = UpdatetblEvoucher.ExpiryDate,
                                        ImagePath = UpdatetblEvoucher.ImagePath,
                                        VoncherAmount = UpdatetblEvoucher.VoucherAmount,
                                        VoucherNo = UpdatetblEvoucher.VoucherNo,
                                        VoucherGenerated = false,
                                    };

                                    _purchaseOrderrepo.Insert(order);

                                    dbContextTransaction.Commit();
                                    response.OrderNo = order.PurchaseOrderNo;
                                    response.IsPurchaseSuccess = true;
                                }

                            }
                        }
                    }
                    catch (Exception e)
                    {
                        response.StatusCode = 500;
                        response.ErrorType = "validation-error";
                        response.ErrorMessage = e.Message;

                        dbContextTransaction.Rollback();
                    }
                }
            }
            else
            {
                response.StatusCode = 400;
                response.ErrorType = "validation-error";
                response.ErrorMessage = validateMsg;
            }
            if (validateMsg != "")
            {
                response.StatusCode = 400;
                response.ErrorType = "validation-error";
                response.ErrorMessage = validateMsg;
            }

            return response;
        }

        public string ValidateBuyEVoucher(BuyEVoucherRequest _request)
        {
            string validationMsg = "";

            var isValidPaymentMethod = (from p in _paymentmethodrepo.Get
                                        where p.PaymentMethod == _request.PaymentMethod
                                        select true).FirstOrDefault();
            if (!isValidPaymentMethod)
            {
                validationMsg = "Invalid Payment Method.";
            }

            return validationMsg;
        }

        public void ScheduleGeneratePromoCode(GeneratePromoCodeRequest _requestData)
        {
            var serviceURL = _configuration["PromoCodeServiceURL"];
            var client = new RestClient(serviceURL);
            var request = new RestRequest(serviceURL + "/api/PromoService/GeneratePromoCode");
            request.Method = Method.POST;
            request.AddJsonBody(_requestData);
            var resp = client.Execute(request);

        }

        public void ScheduleUpdatePaymentHistory()
        {
            var noHistoryOrder = (from o in _purchaseOrderrepo.Get
                                  join gp in _generatedEvoucherrepo.Get
                                  on o.PurchaseOrderNo equals gp.PurchaseOrderNo
                                  join h in _purchaseHistoryrepo.Get
                                  on o.PurchaseOrderNo equals h.PurchaseOrderNo
                                  into lh
                                  from jlh in lh.DefaultIfEmpty()
                                  where jlh == null && o.Status == (int)RecordStatus.Active
                                  select new PurchaseHistoryTb
                                  {
                                      PurchaseOrderNo = gp.PurchaseOrderNo,
                                      PromoCode = gp.PromoCode,
                                      VoucherNo = gp.VoucherNo,
                                      Status = gp.Status,
                                      PurchaseDate = o.OrderDate,

                                  }).ToList();

            if (noHistoryOrder != null && noHistoryOrder.Count > 0)
            {
                _purchaseHistoryrepo.InsertList(noHistoryOrder);
            }
        }

        public void MyJob()
        {
            Thread.Sleep(20000);
        }

    }
}
